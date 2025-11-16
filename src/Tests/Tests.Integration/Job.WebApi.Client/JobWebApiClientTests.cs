using System.Text;
using Job.Broker.Clients;
using Job.Contract;
using Job.Database.Contexts;
using Job.WebApi.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Broker.Abstractions;
using Shared.Broker.Options;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database;

namespace Tests.Integration.Job.WebApi.Client;

/// <summary>
/// Tests for <see cref="JobWebApiClient"/>
/// </summary>
[TestFixture]
internal class JobWebApiClientTests : IntegrationTestBase
{
    [Test]
    public async Task CreateNewJob_ShouldReturnJobId_AndCreateJobInDatabase()
    {
        // arrange
        var expectedTimeout = TimeSpan.FromSeconds(10);
        var expectedScript = Convert.ToBase64String(Encoding.UTF8.GetBytes("echo 1"));
        var client = Services.GetRequiredService<JobWebApiClient>();

        // act
        var jobId = await client.CreateNewJobAsync(new CreateJobRequest
        {
            Timeout = expectedTimeout,
            Script = expectedScript
        }, default);

        // assert
        using var context = Services.GetRequiredService<JobDbContext>();
        var actualJob = await context.Jobs.SingleOrDefaultAsync(m => m.Id == jobId);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJob.Id, Is.EqualTo(jobId));
        Assert.That(actualJob.Timeout, Is.EqualTo(expectedTimeout));
        Assert.That(actualJob.Script, Is.EqualTo(expectedScript));
    }

    [Test]
    public async Task GetJobResults_ShouldGetJobResults()
    {
        // arrange
        var expectedTimeout = TimeSpan.FromSeconds(10);
        var expectedScript = Convert.ToBase64String(Encoding.UTF8.GetBytes("echo 1"));
        var client = Services.GetRequiredService<JobWebApiClient>();
        var jobId = await client.CreateNewJobAsync(new CreateJobRequest
        {
            Timeout = expectedTimeout,
            Script = expectedScript
        }, default);

        // act
        var actualJobResults = await client.GetJobResultsAsync(jobId, default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(actualJobResults.Status, Is.EqualTo(JobStatus.New));
        Assert.That(actualJobResults.Results, Is.Null);
        Assert.That(actualJobResults.StartedAt, Is.Null);
        Assert.That(actualJobResults.FinishedAt, Is.Null);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);

        builder.Services.AddSingleton(builder.Configuration.GetOptions<JobWebApiClientOptions>());
        builder.Services.AddSingleton<IFlurlClientFactory, FlurlClientFactory>();
        builder.Services.AddSingleton<JobWebApiClient>();

        var adminDbOptions = builder.Configuration.GetOptions<DatabaseOptions>("AdminJobsDatabaseOptions");
        var adminSslValidator = new SslValidator(adminDbOptions);
        builder.Services.AddDbContext<JobDbContext>(
            options => PostgreDbContext.BuildOptions(options, adminDbOptions, adminSslValidator, forTests: true),
            ServiceLifetime.Transient);

        builder.Services.AddSingleton(builder.Configuration.GetOptions<AdminOptions>());
        builder.Services.AddTransient<IBrokerAdminClient, BrokerAdminClient>();
    }
}
