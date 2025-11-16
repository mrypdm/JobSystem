using Job.Contract;
using Job.Database.Contexts;
using Job.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database;
using Shared.Database.Migrations;

namespace Tests.Integration.Job.Database;

/// <summary>
/// Tests for <see cref="JobDbContext"/>
/// </summary>
[TestFixture]
internal class JobDbContextTests : IntegrationTestBase
{
    private const string WebApi = nameof(WebApi);
    private const string Worker = nameof(Worker);
    private const string Admin = nameof(Admin);

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

        using var webApiContext = Services.GetRequiredKeyedService<JobDbContext>(WebApi);

        // act
        await webApiContext.AddNewJobAsync(expectedJob, default);

        // assert
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        var actualJob = await adminContext.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob.Id);

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

        using var webApiContext = Services.GetRequiredKeyedService<JobDbContext>(WebApi);
        await webApiContext.AddNewJobAsync(firstJob, default);
        webApiContext.ChangeTracker.Clear();

        // act
        await webApiContext.AddNewJobAsync(secondJob, default);

        // assert
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        var actualJob = await adminContext.Jobs.SingleOrDefaultAsync(m => m.Id == firstJob.Id);

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
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        var actualJob = await workerContext.GetNewJobAsync(expectedJob.Id, default);

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
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);

        // act
        var actualJob = await workerContext.GetNewJobAsync(Guid.NewGuid(), default);

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
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        var actualJob = await workerContext.GetNewJobAsync(expectedJob.Id, default);

        // assert
        Assert.That(actualJob, Is.Null);
    }

    [Test]
    public async Task SetJobRunning_ShouldChangeStatusToRunning()
    {
        // arrange
        var expectedJob = CreateTestJob();
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);
        adminContext.ChangeTracker.Clear();

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        await workerContext.SetJobRunningAsync(expectedJob.Id, default);

        // assert
        var actualJob = await adminContext.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob.Id);
        Assert.That(actualJob.Status, Is.EqualTo(JobStatus.Running));
    }

    [Test]
    public async Task SetJobRunning_AlreadyRunning_ShouldBeIndempotent()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Running);
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        await workerContext.SetJobRunningAsync(expectedJob.Id, default);

        // assert
        var actualJob = await adminContext.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob.Id);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob.Status, Is.EqualTo(JobStatus.Running));
        Assert.That(actualJob.StartedAt, Is.Null); // null, because we change status w/o procedure
    }

    [Test]
    public async Task SetJobRunning_AlreadyFinished_ShouldThrow()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Finished);
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => workerContext.SetJobRunningAsync(expectedJob.Id, default));

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

        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);
        adminContext.ChangeTracker.Clear();

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        await workerContext.SetJobResultsAsync(expectedJob.Id, JobStatus.Finished, expectedResults, default);

        // assert
        var actualJob = await adminContext.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob.Id);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob.Status, Is.EqualTo(JobStatus.Finished));
        Assert.That(actualJob.Results, Is.EqualTo(expectedResults).AsCollection);
    }

    [Test]
    public async Task SetJobResults_JobIsFinishedAlready_ShouldThrow()
    {
        // arrange
        var expectedJob = CreateTestJob(JobStatus.Finished);
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => workerContext.SetJobResultsAsync(expectedJob.Id, JobStatus.Finished, [], default));

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

        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => workerContext.SetJobResultsAsync(expectedJob.Id, JobStatus.Finished, expectedResults, default));

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
        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);

        // act
        using var workerContext = Services.GetRequiredKeyedService<JobDbContext>(Worker);
        var exception = Assert.ThrowsAsync<PostgresException>(
            () => workerContext.SetJobResultsAsync(expectedJob.Id, jobStatus, [], default));

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

        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob);

        // act
        using var webApiContext = Services.GetRequiredKeyedService<JobDbContext>(WebApi);
        var jobResults = await webApiContext.GetJobResultsAsync(expectedJob.Id, default);

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
        using var webApiContext = Services.GetRequiredKeyedService<JobDbContext>(WebApi);

        // act
        var jobResults = await webApiContext.GetJobResultsAsync(Guid.NewGuid(), default);

        // assert
        Assert.That(jobResults, Is.Null);
    }

    [Test]
    public async Task MarkLostJobs_ShouldMarkJobsAsLost()
    {
        // arrange
        var expectedJob1 = CreateTestJob(JobStatus.Running, createdAt: DateTime.UtcNow.AddDays(-1));
        var expectedJob2 = CreateTestJob(JobStatus.Running, createdAt: DateTime.UtcNow);

        using var adminContext = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await AddJobAsync(adminContext, expectedJob1);
        await AddJobAsync(adminContext, expectedJob2);
        adminContext.ChangeTracker.Clear();

        // act
        using var webApiContext = Services.GetRequiredKeyedService<JobDbContext>(WebApi);
        await webApiContext.MarkLostJobsAsync(TimeSpan.FromHours(1), default);

        // assert
        var actualJob1 = await adminContext.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob1.Id);
        var actualJob2 = await adminContext.Jobs.SingleOrDefaultAsync(m => m.Id == expectedJob2.Id);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob1.Status, Is.EqualTo(JobStatus.Lost));
        Assert.That(actualJob2.Status, Is.EqualTo(JobStatus.Running));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);

        var webApiDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("WebApiDatabaseOptions");
        var webApiSslValidator = new SslValidator(webApiDbOptions);
        builder.Services.AddKeyedTransient(WebApi, (context, _) =>
        {
            var options = PostgreDbContext.BuildOptions(
                new DbContextOptionsBuilder(),
                webApiDbOptions,
                webApiSslValidator,
                context.GetRequiredService<ILoggerFactory>(),
                forTests: true);
            return new JobDbContext(options.Options, context.GetRequiredService<ILogger<JobDbContext>>());
        });

        var workerDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("WorkerDatabaseOptions");
        var workerSslValidator = new SslValidator(workerDbOptions);
        builder.Services.AddKeyedTransient(Worker, (context, _) =>
        {
            var options = PostgreDbContext.BuildOptions(
                new DbContextOptionsBuilder(),
                workerDbOptions,
                workerSslValidator,
                context.GetRequiredService<ILoggerFactory>(),
                forTests: true);
            return new JobDbContext(options.Options, context.GetRequiredService<ILogger<JobDbContext>>());
        });

        var adminDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("AdminJobsDatabaseOptions");
        var adminSslValidator = new SslValidator(adminDbOptions);
        builder.Services.AddKeyedTransient(Admin, (context, _) =>
        {
            var options = PostgreDbContext.BuildOptions(
                new DbContextOptionsBuilder(),
                adminDbOptions,
                adminSslValidator,
                context.GetRequiredService<ILoggerFactory>(),
                forTests: true);
            return new JobDbContext(options.Options, context.GetRequiredService<ILogger<JobDbContext>>());
        });
        builder.Services.AddTransient<IInitializer>(
            context => new DbInitializer(context.GetRequiredKeyedService<JobDbContext>(Admin)));
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
