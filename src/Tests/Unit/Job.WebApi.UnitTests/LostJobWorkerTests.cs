using Job.Database.Contexts;
using Job.WebApi.Options;
using Job.WebApi.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shared.Contract.Owned;
using Tests.Common;

namespace Job.WebApi.UnitTests;

/// <summary>
/// Test for <see cref="LostJobWorker"/>
/// </summary>
internal class LostJobWorkerTests : TestBase
{
    private readonly Mock<IJobDbContext> _jobDbContext = new();
    private readonly Mock<IOwnedService<IJobDbContext>> _jobDbContextOwned = new();
    private readonly LostJobWorkerOptions _lostJobWorkerOptions = new()
    {
        IterationDeplay = TimeSpan.FromSeconds(15),
        LostTimeoutForJobs = TimeSpan.FromHours(1)
    };

    [SetUp]
    public void SetUp()
    {
        _jobDbContext.Reset();
        _jobDbContextOwned.Reset();

        _jobDbContextOwned
            .Setup(m => m.Value)
            .Returns(_jobDbContext.Object);
    }

    [Test]
    public async Task RunAsync_ShouldStartLoop_AndMarkLostJobInIt()
    {
        // arrange

        var worker = Services.GetRequiredService<LostJobWorker>();

        _jobDbContext
            .Setup(m => m.MarkLostJobsAsync(_lostJobWorkerOptions.LostTimeoutForJobs, It.IsAny<CancellationToken>()))
            .Callback(() => _ = worker.StopAsync(default));

        // act
        await worker.StartAsync(default);
        await Task.Delay(TimeSpan.FromSeconds(5));

        // assert
        _jobDbContext.Verify(
            m => m.MarkLostJobsAsync(_lostJobWorkerOptions.LostTimeoutForJobs, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddSingleton(_jobDbContextOwned.Object);
        builder.Services.AddSingleton(_lostJobWorkerOptions);
        builder.Services.AddScoped<LostJobWorker>();
    }
}
