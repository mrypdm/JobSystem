using Job.Contract;
using Job.Worker.JobProcesses;
using Job.Worker.Models;
using Job.Worker.Processes;
using Moq;

namespace Job.Worker.Tests;

/// <summary>
/// Tests for <see cref="DockerJobProcessRunner"/>
/// </summary>
[TestFixture]
internal class DockerJobProcessRunnerTests : TestBase
{
    private readonly Mock<IProcessRunner> _runner = new();

    [SetUp]
    public void SetUp()
    {
        _runner.Reset();
    }

    [Test]
    public void RunProcess_NullJob_Throw()
    {
        // arrange
        var runner = CreateRunner();

        // act & assert
        Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunProcessAsync(null));
    }

    [Test]
    public void RunProcess_JobDirectoryNotSet_Throw()
    {
        // arrange
        var runner = CreateRunner();

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

        var runner = CreateRunner();

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
        return RunProcessWithResult(JobStatus.Timeout, new OperationCanceledException());
    }

    [Test]
    public Task RunProcess_Failed_ShouldFault()
    {
        return RunProcessWithResult(JobStatus.Fault, new Exception());
    }

    private async Task RunProcessWithResult(JobStatus jobStatus, Exception exception)
    {
        // arrange
        var jobModel = new RunJobModel
        {
            Id = Guid.NewGuid(),
            Directory = "some-dir",
            Timeout = TimeSpan.FromHours(1),
        };

        var dockerUpCommand = new string[] { "docker", "compose", "up" };
        var dockerDownCommand = new string[] { "docker", "compose", "down", "-t", "10" };

        if (exception is not null)
        {
            _runner
                .Setup(m => m.RunProcessAsync(It.Is<string[]>(m => m.SequenceEqual(dockerUpCommand)), jobModel.Directory,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
        }

        var runner = CreateRunner();

        // act
        await runner.RunProcessAsync(jobModel);

        // assert
        Assert.That(jobModel.Status, Is.EqualTo(jobStatus));

        _runner.Verify(
            m => m.RunProcessAsync(It.Is<string[]>(m => m.SequenceEqual(dockerUpCommand)), jobModel.Directory,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _runner.Verify(
            m => m.RunProcessAsync(It.Is<string[]>(m => m.SequenceEqual(dockerDownCommand)), jobModel.Directory,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private DockerJobProcessRunner CreateRunner()
    {
        return new DockerJobProcessRunner(_runner.Object, CreateLogger<DockerJobProcessRunner>());
    }
}
