using System.Runtime.InteropServices;
using System.Text;
using Job.Worker.Environments;
using Job.Worker.Models;
using Job.Worker.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tests.Common;

namespace Tests.Unit.Job.Worker;

/// <summary>
/// Tests for <see cref="LinuxDockerJobEnvironment"/>
/// </summary>
[TestFixture]
internal class LinuxDockerJobEnvironmentTests : TestBase
{
    private readonly JobEnvironmentOptions _jobEnvironmentOptions = new()
    {
        CpuUsage = 0.5,
        MemoryUsage = 500,
        JobsDirectory = "TestData/worker"
    };

    [Test]
    public void PrepareEnvironment_NullJob_Throw()
    {
        // arrange
        var environment = Services.GetRequiredService<LinuxDockerJobEnvironment>();

        // act & assert
        Assert.Throws<ArgumentNullException>(() => environment.PrepareEnvironment(null));
    }

    [Test]
    public void PrepareEnvironment_JobDirectoryIsSet_Throw()
    {
        // arrange
        var jobModel = new RunJobModel()
        {
            Directory = "dir"
        };

        var environment = Services.GetRequiredService<LinuxDockerJobEnvironment>();

        // act & assert
        Assert.Throws<InvalidOperationException>(() => environment.PrepareEnvironment(jobModel));
    }

    [Test]
    public void PrepareEnvironment_JobScriptIsNotSet_Throw()
    {
        // arrange
        var environment = Services.GetRequiredService<LinuxDockerJobEnvironment>();

        // act & assert
        Assert.Throws<InvalidOperationException>(() => environment.PrepareEnvironment(new RunJobModel()));
    }

    [Test]
    public void PrepareEnvironment_ShouldCreateEnvironemnt()
    {
        // arrange
        _jobEnvironmentOptions.JobsDirectory = Path.GetFullPath("TestData/worker").Replace("\\", "/");

        var expectedScript = "hello, world";
        var jobModel = new RunJobModel()
        {
            Id = Guid.Parse("02c6a6eb-92ae-49d5-8743-1dded645d705"),
            Script = Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedScript))
        };
        var expectedDir = Path.Combine(_jobEnvironmentOptions.JobsDirectory, jobModel.Id.ToString()).Replace("\\", "/");

        var environment = Services.GetRequiredService<LinuxDockerJobEnvironment>();

        // act
        environment.PrepareEnvironment(jobModel);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(jobModel.Directory, Is.EqualTo(expectedDir));
        Assert.That(Path.Combine(jobModel.Directory, "stdout.txt"), Does.Exist);
        Assert.That(Path.Combine(jobModel.Directory, "stderr.txt"), Does.Exist);
        Assert.That(Path.Combine(jobModel.Directory, "docker-compose.yaml"), Does.Exist);
        Assert.That(Path.Combine(jobModel.Directory, "run.sh"), Does.Exist);

        var actualScript = File.ReadAllText(Path.Combine(jobModel.Directory, "run.sh"));
        Assert.That(actualScript, Is.EqualTo(expectedScript));

        var actualDocker = File.ReadAllText(Path.Combine(jobModel.Directory, "docker-compose.yaml"));
        var expectedDocker = File.ReadAllText(Path.Combine("TestData/worker", "docker-compose.yaml.expected"))
            .Replace("<JOB-DIR>", _jobEnvironmentOptions.JobsDirectory);
        Assert.That(actualDocker, Is.EqualTo(expectedDocker));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.That(
                File.GetUnixFileMode(Path.Combine(jobModel.Directory, "run.sh")),
                Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead));
            Assert.That(
                File.GetUnixFileMode(Path.Combine(jobModel.Directory, "stdout.txt")),
                Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherWrite));
            Assert.That(
                File.GetUnixFileMode(Path.Combine(jobModel.Directory, "stderr.txt")),
                Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherWrite));
        }
    }

    [Test]
    public void PrepareEnvironment_EnvironmentExist_ShouldDeleteEnvironment()
    {
        // arrange
        using var tempDir = Services.GetRequiredService<TempDirectory>();
        _jobEnvironmentOptions.JobsDirectory = tempDir.Path;
        var environment = Services.GetRequiredService<LinuxDockerJobEnvironment>();

        var jobId = Guid.NewGuid();
        var expectedDir = Path.Combine(_jobEnvironmentOptions.JobsDirectory, jobId.ToString());
        var expectedFile = Path.Combine(expectedDir, "a.txt");
        Directory.CreateDirectory(expectedDir);
        File.WriteAllText(expectedFile, "test");

        var jobModel = new RunJobModel()
        {
            Id = jobId,
            Script = Convert.ToBase64String(Encoding.UTF8.GetBytes("hello, world"))
        };


        // act
        environment.PrepareEnvironment(jobModel);

        // assert
        Assert.That(expectedFile, Does.Not.Exist);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddSingleton(_jobEnvironmentOptions);
        builder.Services.AddTransient<LinuxDockerJobEnvironment>();
    }
}
