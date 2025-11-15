using Job.Contract;
using Job.Database.Contexts;
using Job.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database;
using Tests.Common;
using Tests.Common.Initializers;

namespace Job.Database.IntegrationTests;

/// <summary>
/// Tests for <see cref="JobDbContext"/>
/// </summary>
internal class JobDbContextTests : IntegrationTestBase
{
    [Test]
    public async Task AddNewJob_ShouldAddNewJob()
    {
        // arrange
        var expectedJob = new NewJobModel
        {
            Id = Guid.NewGuid(),
            Script = "script",
            Timeout = TimeSpan.FromSeconds(5)
        };

        using var context = Services.GetRequiredService<JobDbContext>();

        // act
        await context.AddNewJobAsync(expectedJob, default);

        // assert
        var actualJob = await context.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob.Id);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob, Is.Not.Null);
        Assert.That(actualJob.Id, Is.EqualTo(expectedJob.Id));
        Assert.That(actualJob.Status, Is.EqualTo(JobStatus.New));
        Assert.That(actualJob.Timeout, Is.EqualTo(expectedJob.Timeout));
        Assert.That(actualJob.Script, Is.EqualTo(expectedJob.Script));
        Assert.That(actualJob.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(3)));
        Assert.That(actualJob.StartedAt, Is.Null);
        Assert.That(actualJob.FinishedAt, Is.Null);
        Assert.That(actualJob.Results, Is.Null);
    }

    [Test]
    public async Task AddNewJob_Duplicate_ShouldNotUpdate()
    {
        // arrange
        var firstJob = new NewJobModel
        {
            Id = Guid.NewGuid(),
            Script = "script",
            Timeout = TimeSpan.FromSeconds(5)
        };
        var secondJob = new NewJobModel
        {
            Id = firstJob.Id,
            Script = "script1",
            Timeout = TimeSpan.FromSeconds(10)
        };

        using var context = Services.GetRequiredService<JobDbContext>();
        await context.AddNewJobAsync(firstJob, default);
        context.ChangeTracker.Clear();

        // act
        await context.AddNewJobAsync(secondJob, default);

        // assert
        var actualJob = await context.Jobs.SingleOrDefaultAsync(m => m.Id == firstJob.Id);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob, Is.Not.Null);
        Assert.That(actualJob.Id, Is.EqualTo(firstJob.Id));
        Assert.That(actualJob.Timeout, Is.EqualTo(firstJob.Timeout));
        Assert.That(actualJob.Script, Is.EqualTo(firstJob.Script));
    }

    [Test]
    public async Task GetNewJob_ShouldReturnAddedJob()
    {
        // arrange
        var expectedJob = CreateTestJob();
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        var actualJob = await context.GetNewJobAsync(expectedJob.Id, default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob, Is.Not.Null);
        Assert.That(actualJob.Id, Is.EqualTo(expectedJob.Id));
        Assert.That(actualJob.Timeout, Is.EqualTo(expectedJob.Timeout));
        Assert.That(actualJob.Script, Is.EqualTo(expectedJob.Script));
    }

    [Test]
    public async Task GetNewJob_NotExists_ShouldReturnNull()
    {
        // arrange
        using var context = Services.GetRequiredService<JobDbContext>();

        // act
        var actualJob = await context.GetNewJobAsync(Guid.NewGuid(), default);

        // assert
        Assert.That(actualJob, Is.Null);
    }

    [Test]
    [TestCase(JobStatus.Running)]
    [TestCase(JobStatus.Finished)]
    [TestCase(JobStatus.Lost)]
    public async Task GetNewJob_RunningOrFinished_ShouldReturnNull(JobStatus jobStatus)
    {
        // arrange
        var expectedJob = CreateTestJob(jobStatus);
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        var actualJob = await context.GetNewJobAsync(expectedJob.Id, default);

        // assert
        Assert.That(actualJob, Is.Null);
    }

    [Test]
    public async Task SetJobRunning_ShouldChangeStatusToRunning()
    {
        // arrange
        var expectedJob = CreateTestJob();
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        await context.SetJobRunningAsync(expectedJob.Id, default);

        // assert
        var actualJob = await context.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob.Id);
        Assert.That(actualJob.Status, Is.EqualTo(JobStatus.Running));
    }

    [Test]
    public async Task SetJobRunning_AlreadyRunning_ShouldBeIndempotent()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Running);
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        await context.SetJobRunningAsync(expectedJob.Id, default);

        // assert
        var actualJob = await context.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob.Id);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob.Status, Is.EqualTo(JobStatus.Running));
        Assert.That(actualJob.StartedAt, Is.Null); // null, because we change status w/o procedure
    }

    [Test]
    public async Task SetJobRunning_AlreadyFinished_ShouldThrow()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Finished);
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => context.SetJobRunningAsync(expectedJob.Id, default));

        // assert
        Assert.That(
            exception.Message,
            Does.Contain($"Job {expectedJob.Id} is finished with status {(int)expectedJob.Status}"));
    }

    [Test]
    public async Task SetJobResults_ShouldSetResults()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Running);
        var expectedResults = new byte[] { 0x00, 0x11 };

        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        await context.SetJobResultsAsync(expectedJob.Id, JobStatus.Finished, expectedResults, default);

        // assert
        var actualJob = await context.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob.Id);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob.Status, Is.EqualTo(JobStatus.Finished));
        Assert.That(actualJob.Results, Is.EqualTo(expectedResults).AsCollection);
    }

    [Test]
    public async Task SetJobResults_JobIsFinishedAlready_ShouldThrow()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Finished);
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => context.SetJobResultsAsync(expectedJob.Id, JobStatus.Finished, [], default));

        // assert
        Assert.That(
            exception.Message,
            Does.Contain($"Job {expectedJob.Id} is already finished with status {(int)expectedJob.Status}"));
    }

    [Test]
    public async Task SetJobResults_LargeResults_ShouldThrow()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Running);
        var expectedResults = new byte[60 * 1024 * 1024]; // 60 MB

        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => context.SetJobResultsAsync(expectedJob.Id, JobStatus.Finished, expectedResults, default));

        // assert
        Assert.That(
            exception.Message,
            Does.Contain("new row for relation \"Jobs\" violates check constraint"));
    }

    [Test]
    [TestCase(JobStatus.Running)]
    [TestCase((JobStatus)6)]
    public async Task SetJobResults_InvalidStatus_ShouldThrow(JobStatus jobStatus)
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Running);
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => context.SetJobResultsAsync(expectedJob.Id, jobStatus, [], default));

        // assert
        Assert.That(
            exception.Message,
            Does.Contain("Status must be >= 2 (Finished) and <= 5 (Lost)"));
    }

    [Test]
    public async Task GetJobResults_ShouldGetResults()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Finished, DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1), [0x00, 0x11]);
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob);
        context.ChangeTracker.Clear();

        // act
        var jobResults = await context.GetJobResultsAsync(expectedJob.Id, default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(jobResults, Is.Not.Null);
        Assert.That(jobResults.Status, Is.EqualTo(expectedJob.Status));
        Assert.That(jobResults.Results, Is.EqualTo(expectedJob.Results).AsCollection);
        Assert.That(jobResults.StartedAt, Is.EqualTo(expectedJob.StartedAt.Value).Within(TimeSpan.FromMicroseconds(1)));
        Assert.That(jobResults.FinishedAt, Is.EqualTo(expectedJob.FinishedAt).Within(TimeSpan.FromMicroseconds(1)));
    }

    [Test]
    public async Task GetJobResults_JobNotExists_ShouldReturnNull()
    {
        // arrange
        using var context = Services.GetRequiredService<JobDbContext>();

        // act
        var jobResults = await context.GetJobResultsAsync(Guid.NewGuid(), default);

        // assert
        Assert.That(jobResults, Is.Null);
    }

    [Test]
    public async Task MarkLostJobs_ShouldMarkJobsAsLost()
    {
        // arrange
        var expectedJob1 = CreateTestJob(JobStatus.Running, createdAt: DateTime.UtcNow.AddDays(-1));
        var expectedJob2 = CreateTestJob(JobStatus.Running, createdAt: DateTime.UtcNow);
        using var context = Services.GetRequiredService<JobDbContext>();
        await AddJobAsync(context, expectedJob1);
        await AddJobAsync(context, expectedJob2);
        context.ChangeTracker.Clear();

        // act
        await context.MarkLostJobsAsync(TimeSpan.FromHours(1), default);

        // assert
        var actualJob1 = await context.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob1.Id);
        var actualJob2 = await context.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob2.Id);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob1.Status, Is.EqualTo(JobStatus.Lost));
        Assert.That(actualJob2.Status, Is.EqualTo(JobStatus.Running));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);

        var dbOptions = builder.Configuration.GetOptions<DatabaseOptions>();
        var sslValidator = new SslValidator(dbOptions);
        builder.Services.AddDbContext<JobDbContext>(
            options => PostgreDbContext
                .BuildOptions(options, dbOptions, sslValidator)
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging(),
            ServiceLifetime.Transient);
        builder.Services.AddTransient<IInitializer>(
            context => new DbInitializer(context.GetRequiredService<JobDbContext>()));
    }

    private static JobDbModel CreateTestJob(
        JobStatus status = JobStatus.New,
        DateTime? startedAt = null,
        DateTime? finishedAt = null,
        byte[] results = null,
        DateTime? createdAt = null)
    {
        return new JobDbModel
        {
            Id = Guid.NewGuid(),
            Script = "script",
            Timeout = TimeSpan.FromSeconds(5),
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            Results = results
        };
    }

    private static async Task AddJobAsync(JobDbContext context, JobDbModel jobDbModel)
    {
        await context.AddAsync(jobDbModel);
        await context.SaveChangesAsync();
    }
}
