using Job.Database.Contexts;
using Job.WebApi.Options;
using Job.WebApi.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shared.Contract.Owned;
using Tests.Common;

namespace Tests.Unit.Job.WebApi;

/// <summary>
/// Test for <see cref="LostJobWorker"/>
/// </summary>
[TestFixture]
internal class LostJobWorkerTests : TestBase
{
    private readonly Mock<IJobDbContext> _jobDbContext = new();
    private readonly Mock<IOwnedService<IJobDbContext>> _jobDbContextOwned = new();
    private readonly LostJobWorkerOptions _lostJobWorkerOptions = new()
    {
        IsEnabled = true,
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
    public async Task Run_ShouldStartLoop_AndMarkLostJobInIt()
    {
        // arrange
        _lostJobWorkerOptions.IsEnabled = true;
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

    [Test]
    public async Task Run_Disabled_ShouldDoNothing()
    {
        // arrange
        _lostJobWorkerOptions.IsEnabled = false;
        var worker = Services.GetRequiredService<LostJobWorker>();

        // act
        await worker.StartAsync(default);
        await Task.Delay(TimeSpan.FromSeconds(5));
        await worker.StopAsync(default);

        // assert
        _jobDbContext.Verify(
            m => m.MarkLostJobsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddSingleton(_jobDbContextOwned.Object);
        builder.Services.AddSingleton(_lostJobWorkerOptions);
        builder.Services.AddTransient<LostJobWorker>();
    }
}
