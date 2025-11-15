using System.Runtime.InteropServices;
using System.Text;
using Job.Broker;
using Job.Broker.Clients;
using Job.Contract;
using Job.Database.Contexts;
using Job.Worker.Collectors;
using Job.Worker.Environments;
using Job.Worker.JobProcesses;
using Job.Worker.Processes;
using Job.Worker.Runners;
using Job.Worker.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Broker.Abstractions;
using Shared.Broker.Options;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Contract.Owned;
using Shared.Database;
using Tests.Integration.Initializers;

namespace Tests.Integration;

/// <summary>
/// Tests for <see cref="ConsumerWorker"/>
/// </summary>
internal class ConsumerWorkerTests : IntegrationTestBase
{
    private const string Admin = nameof(Admin);

    [OneTimeSetUp]
    public void CheckOs()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Inconclusive("These test can be run only on Linux");
        }
    }

    [Test]
    public async Task RunAsync_ShouldRunJob_AndSaveResults()
    {
        // arrange
        var jobId = await CreateJobAndPublish("echo \"Running as $(id)\"");

        var worker = Services.GetRequiredService<ConsumerWorker>();
        var runner = Services.GetRequiredService<IJobRunner>();

        // act
        await worker.StartAsync(default);
        await runner.WaitForAllJobs();
        var endTime = DateTime.UtcNow;
        await worker.StopAsync(default);

        // assert
        using var context = Services.GetRequiredService<JobDbContext>();
        var jobResults = await context.GetJobResultsAsync(jobId, default);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(jobResults.Status, Is.EqualTo(JobStatus.Finished));
        Assert.That(jobResults.FinishedAt, Is.Not.Null);
        Assert.That(jobResults.FinishedAt.Value, Is.EqualTo(endTime).Within(TimeSpan.FromSeconds(3)));
        Assert.That($"/tmp/jobs/{jobId}", Does.Not.Exist);
    }

    [Test]
    public async Task RunAsync_Timeout_ShouldStopJob()
    {
        // arrange
        var jobId = await CreateJobAndPublish("sleep 10");

        var worker = Services.GetRequiredService<ConsumerWorker>();
        var runner = Services.GetRequiredService<IJobRunner>();

        // act
        await worker.StartAsync(default);
        await runner.WaitForAllJobs();
        var endTime = DateTime.UtcNow;
        await worker.StopAsync(default);

        // assert
        using var context = Services.GetRequiredService<JobDbContext>();
        var jobResults = await context.GetJobResultsAsync(jobId, default);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(jobResults.Status, Is.EqualTo(JobStatus.Timeout));
        Assert.That(jobResults.FinishedAt, Is.Not.Null);
        Assert.That(jobResults.FinishedAt.Value, Is.EqualTo(endTime).Within(TimeSpan.FromSeconds(3)));
        Assert.That($"/tmp/jobs/{jobId}", Does.Not.Exist);
    }

    [Test]
    public async Task RunAsync_AlreadyFinished_ShouldNotRun()
    {
        // arrange
        var jobId = await CreateJobAndPublish("exit");
        using var context = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await context.SetJobResultsAsync(jobId, JobStatus.Finished, [0x00, 0x11], default);
        var realEndTime = DateTime.UtcNow;

        var worker = Services.GetRequiredService<ConsumerWorker>();
        var runner = Services.GetRequiredService<IJobRunner>();

        // act
        await worker.StartAsync(default);
        await runner.WaitForAllJobs();
        var endTime = DateTime.UtcNow;
        await worker.StopAsync(default);

        // assert
        var jobResults = await context.GetJobResultsAsync(jobId, default);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(jobResults.Status, Is.EqualTo(JobStatus.Finished));
        Assert.That(jobResults.FinishedAt, Is.Not.Null);
        Assert.That(jobResults.FinishedAt.Value, Is.EqualTo(realEndTime).Within(TimeSpan.FromSeconds(3)));
        Assert.That(jobResults.Results, Is.EqualTo([0x00, 0x11]).AsCollection);
        Assert.That($"/tmp/jobs/{jobId}", Does.Not.Exist);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);

        builder.Services.AddSingleton(builder.Configuration.GetOptions<ConsumerOptions>());
        builder.Services.AddSingleton<IJobConsumer<Guid, JobMessage>, JobConsumer>();

        builder.Services.AddSingleton(builder.Configuration.GetOptions<ProducerOptions>());
        builder.Services.AddSingleton<IJobProducer<Guid, JobMessage>, JobProducer>();

        builder.Services.AddSingleton(builder.Configuration.GetOptions<AdminOptions>());
        builder.Services.AddSingleton<IBrokerAdminClient, BrokerAdminClient>();
        builder.Services.AddTransient<IInitializer>(
            context => new BrokerInitializer(context.GetRequiredService<IBrokerAdminClient>()));

        builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
        builder.Services.AddSingleton<IJobProcessRunner, DockerJobProcessRunner>();
        builder.Services.AddSingleton<IResultsCollector, ZipResultsCollector>();
        builder.Services.AddSingleton<IJobEnvironment, LinuxDockerJobEnvironment>();

        var workerDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("WorkerDatabaseOptions");
        var workerSslValidator = new SslValidator(workerDbOptions);
        builder.Services.AddTransient<IJobDbContext>(context =>
        {
            var options = PostgreDbContext
                .BuildOptions(new DbContextOptionsBuilder(), workerDbOptions, workerSslValidator, forTests: true)
                .Options;
            return new JobDbContext(options, context.GetRequiredService<ILogger<JobDbContext>>());
        });
        builder.Services.AddSingleton<IOwnedService<IJobDbContext>, OwnedService<IJobDbContext>>();

        var adminDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("AdminJobsDatabaseOptions");
        var adminSslValidator = new SslValidator(adminDbOptions);
        builder.Services.AddKeyedTransient(Admin, (context, _) =>
        {
            var options = PostgreDbContext
                .BuildOptions(new DbContextOptionsBuilder(), adminDbOptions, adminSslValidator, forTests: true)
                .Options;
            return new JobDbContext(options, context.GetRequiredService<ILogger<JobDbContext>>());
        });
        builder.Services.AddTransient<IInitializer>(
            context => new DbInitializer(context.GetRequiredKeyedService<JobDbContext>(Admin)));

        builder.Services.AddTransient<IJobRunner, JobRunner>();
    }

    private async Task<Guid> CreateJobAndPublish(string script)
    {
        var jobId = Guid.NewGuid();
        var jobTimeout = TimeSpan.FromSeconds(5);
        var jobScript = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));

        using var context = Services.GetRequiredKeyedService<JobDbContext>(Admin);
        await context.AddNewJobAsync(new NewJobModel
        {
            Id = jobId,
            Timeout = jobTimeout,
            Script = jobScript
        }, default);

        using var producer = Services.GetRequiredService<IJobProducer<Guid, JobMessage>>();
        await producer.PublishAsync(new JobMessage { Id = jobId }, default);

        return jobId;
    }
}
