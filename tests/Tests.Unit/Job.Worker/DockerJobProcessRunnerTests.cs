using System.Diagnostics;
using Docker.DotNet;
using Docker.DotNet.Models;
using Job.Worker.JobProcesses;
using Job.Worker.Models;
using Job.Worker.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shared.Contract.Owned;
using Tests.Common;

using JobStatus = Job.Contract.JobStatus;

namespace Tests.Unit.Job.Worker;

/// <summary>
/// Tests for <see cref="DockerJobProcessRunner"/>
/// </summary>
[TestFixture]
internal class DockerJobProcessRunnerTests : TestBase
{
    private readonly Mock<IDockerClient> _dockerClient = new();
    private readonly Mock<IContainerOperations> _containerOperations = new();

    private readonly JobEnvironmentOptions _jobEnvironmentOptions = new()
    {
        CpuUsage = 1,
        MemoryUsage = 1,
        JobsDirectory = "/etc/jobs"
    };

    [SetUp]
    public void SetUp()
    {
        _dockerClient.Reset();
        _containerOperations.Reset();

        _dockerClient
            .Setup(m => m.Containers)
            .Returns(_containerOperations.Object);
    }

    [Test]
    public void RunProcess_NullJob_Throw()
    {
        // arrange
        var runner = Services.GetRequiredService<DockerJobProcessRunner>();

        // act & assert
        Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunProcessAsync(null));
    }

    [Test]
    public void RunProcess_JobDirectoryNotSet_Throw()
    {
        // arrange
        var runner = Services.GetRequiredService<DockerJobProcessRunner>();

        // act & assert
        Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunProcessAsync(new RunJobModel()));
    }

    [Test]
    public void RunProcess_JobIsFinished_Throw()
    {
        // arrange
        var jobModel = new RunJobModel()
        {
            Directory = "dir",
            Status = JobStatus.Finished
        };

        var runner = Services.GetRequiredService<DockerJobProcessRunner>();

        // act & assert
        Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunProcessAsync(jobModel));
    }

    [Test]
    public Task RunProcess_ShouldStartAndWait()
    {
        return RunProcessWithResult(JobStatus.Finished, exception: null);
    }

    [Test]
    public Task RunProcess_Timeouted_ShouldTimeout()
    {
        return RunProcessWithResult(JobStatus.Timeout, new OperationCanceledException("Exception by Test"));
    }

    [Test]
    public Task RunProcess_Failed_ShouldFault()
    {
        return RunProcessWithResult(JobStatus.Fault, new UnreachableException("Exception by Test"));
    }

    private async Task RunProcessWithResult(JobStatus jobStatus, Exception exception)
    {
        // arrange
        const string containerId = nameof(containerId);

        var jobModel = new RunJobModel
        {
            Id = Guid.NewGuid(),
            Directory = "some-dir",
            Timeout = TimeSpan.FromHours(1),
        };

        _containerOperations
            .Setup(m => m.CreateContainerAsync(It.IsAny<CreateContainerParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateContainerResponse() { ID = containerId, Warnings = [] });

        _containerOperations
            .Setup(m => m.StartContainerAsync(containerId, It.IsAny<ContainerStartParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        if (exception is not null)
        {
            _containerOperations
                .Setup(m => m.WaitContainerAsync(containerId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
        }

        var runner = Services.GetRequiredService<DockerJobProcessRunner>();

        // act
        await runner.RunProcessAsync(jobModel);

        // assert
        Assert.That(jobModel.Status, Is.EqualTo(jobStatus));

        _dockerClient.Verify(m => m.Containers, Times.Exactly(5));

        _containerOperations.Verify(
            m => m.CreateContainerAsync(
                It.Is<CreateContainerParameters>(args =>
                    args.Image == "alpine"
                    && args.Name == $"job_{jobModel.Id}"
                    && args.User == "10000:10000"
                    && args.HostConfig.NanoCPUs == _jobEnvironmentOptions.CpuUsage * 1_000_000_000
                    && args.HostConfig.Memory == _jobEnvironmentOptions.MemoryUsage * 1024 * 1024
                    && args.HostConfig.RestartPolicy.Name == RestartPolicyKind.No
                    && args.HostConfig.Binds.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _containerOperations.Verify(
            m => m.StartContainerAsync(containerId, It.IsAny<ContainerStartParameters>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _containerOperations.Verify(
            m => m.WaitContainerAsync(containerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _containerOperations.Verify(
            m => m.StopContainerAsync(containerId,
                It.Is<ContainerStopParameters>(args => args.WaitBeforeKillSeconds == 10),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _containerOperations.Verify(
            m => m.RemoveContainerAsync(containerId,
                It.Is<ContainerRemoveParameters>(args => args.Force == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddSingleton(_jobEnvironmentOptions);
        builder.Services.AddSingleton(_dockerClient.Object);
        builder.Services.AddSingleton<IOwnedService<IDockerClient>, OwnedService<IDockerClient>>();
        builder.Services.AddTransient<DockerJobProcessRunner>();
    }
}
