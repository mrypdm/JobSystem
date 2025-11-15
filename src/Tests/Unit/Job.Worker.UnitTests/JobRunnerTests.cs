using Job.Database.Contexts;
using Job.Worker.Collectors;
using Job.Worker.Environments;
using Job.Worker.JobProcesses;
using Job.Worker.Models;
using Job.Worker.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shared.Contract.Owned;
using Tests.Common;

namespace Job.Worker.UnitTests;

/// <summary>
/// Tests for <see cref="JobRunner"/>
/// </summary>
[TestFixture]
internal class JobRunnerTests : TestBase
{
    private readonly Mock<IJobDbContext> _jobDbContext = new();
    private readonly Mock<IOwnedService<IJobDbContext>> _jobDbContextOwned = new();
    private readonly Mock<IJobEnvironment> _jobEnvironment = new();
    private readonly Mock<IJobProcessRunner> _jobProcessRunner = new();
    private readonly Mock<IResultsCollector> _resultsCollector = new();

    [SetUp]
    public void SetUp()
    {
        _jobDbContext.Reset();
        _jobDbContextOwned.Reset();
        _jobEnvironment.Reset();
        _jobProcessRunner.Reset();
        _resultsCollector.Reset();

        _jobDbContextOwned
            .Setup(m => m.Value)
            .Returns(_jobDbContext.Object);
    }

    [Test]
    public void RunJob_NullJob_Throw()
    {
        // arrange
        var runner = Services.GetRequiredService<JobRunner>();

        // act & assert
        Assert.Throws<ArgumentNullException>(() => runner.RunJob(null));
    }

    [Test]
    public async Task RunJob_DuplicateJob_ShouldNotRun()
    {
        // arrange
        var jobModel = new RunJobModel();

        _jobDbContext
            .Setup(m => m.SetJobResultsAsync(jobModel.Id, jobModel.Status, jobModel.Results, It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(1000));

        var runner = Services.GetRequiredService<JobRunner>();

        // act
        runner.RunJob(jobModel);
        Assert.That(runner.RunningJobsCount, Is.EqualTo(1));
        runner.RunJob(jobModel);
        Assert.That(runner.RunningJobsCount, Is.EqualTo(1));
        await runner.WaitForAllJobs();

        // assert
        Assert.That(runner.RunningJobsCount, Is.Zero);
    }

    [Test]
    public async Task RunJob_ShouldDeleteJobAfterComplete()
    {
        // arrange
        var jobModel = new RunJobModel();

        _jobDbContext
            .Setup(m => m.SetJobResultsAsync(jobModel.Id, jobModel.Status, jobModel.Results, It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(1000));

        var runner = Services.GetRequiredService<JobRunner>();

        // act
        runner.RunJob(jobModel);
        Assert.That(runner.RunningJobsCount, Is.EqualTo(1));
        await runner.WaitForAllJobs();

        // assert
        Assert.That(runner.RunningJobsCount, Is.Zero);
    }

    [Test]
    public async Task RunJob_ExceptionThrown_ShouldSetFaultResult()
    {
        // arrange
        var jobModel = new RunJobModel();

        _jobEnvironment
            .Setup(m => m.PrepareEnvironment(It.IsAny<RunJobModel>()))
            .Throws(new Exception("Bad excepiton"));

        var runner = Services.GetRequiredService<JobRunner>();

        // act
        runner.RunJob(jobModel);
        Assert.That(runner.RunningJobsCount, Is.EqualTo(1));
        await runner.WaitForAllJobs();

        // assert
        Assert.That(runner.RunningJobsCount, Is.Zero);
        _jobDbContext.Verify(
            m => m.SetJobResultsAsync(jobModel.Id, Contract.JobStatus.Fault, Array.Empty<byte>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RunJob_ShouldCallInCorrectOrder()
    {
        // arrange
        var jobModel = new RunJobModel();

        var order = 0;
        _jobEnvironment
            .Setup(m => m.PrepareEnvironment(jobModel))
            .Callback(() => Assert.That(++order, Is.EqualTo(1)));
        _jobProcessRunner
            .Setup(m => m.RunProcessAsync(jobModel))
            .Callback(() => Assert.That(++order, Is.EqualTo(2)));
        _resultsCollector
            .Setup(m => m.CollectResultsAsync(jobModel))
            .Callback(() => Assert.That(++order, Is.EqualTo(3)));
        _jobDbContext
            .Setup(m => m.SetJobResultsAsync(jobModel.Id, jobModel.Status, jobModel.Results, It.IsAny<CancellationToken>()))
            .Callback(() => Assert.That(++order, Is.EqualTo(4)))
            .Returns(Task.Delay(1000));
        _jobEnvironment
            .Setup(m => m.ClearEnvironment(jobModel))
            .Callback(() => Assert.That(++order, Is.EqualTo(5)));

        var runner = Services.GetRequiredService<JobRunner>();

        // act
        runner.RunJob(jobModel);
        await runner.WaitForAllJobs();

        // assert
        Assert.That(order, Is.EqualTo(5));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddSingleton(_jobDbContextOwned.Object);
        builder.Services.AddSingleton(_jobEnvironment.Object);
        builder.Services.AddSingleton(_jobProcessRunner.Object);
        builder.Services.AddSingleton(_resultsCollector.Object);
        builder.Services.AddTransient<JobRunner>();
    }
}
