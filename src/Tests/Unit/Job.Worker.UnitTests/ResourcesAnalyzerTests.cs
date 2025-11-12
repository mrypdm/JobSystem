using Job.Worker.Options;
using Job.Worker.Resources.Analyzers;
using Job.Worker.Resources.Models;
using Job.Worker.Resources.Readers;
using Job.Worker.Runners;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Common;

namespace Job.Worker.Tests;

/// <summary>
/// Tests for <see cref="ResourcesAnalyzer"/>
/// </summary>
[TestFixture]
internal class ResourcesAnalyzerTests : TestBase
{
    private readonly Mock<IJobRunner> _jobRunner = new();
    private readonly Mock<IResourcesReader> _resourcesReader = new();
    private readonly JobEnvironmentOptions _jobEnvironmentOptions = new()
    {
        CpuUsage = 0.5,
        MemoryUsage = 500,
        JobsDirectory = "dir"
    };
    private readonly ResourcesAnalyzerOptions _resourceMonitorOptions = new()
    {
        ThresholdCpuUsage = 0.8,
        ThresholdMemoryUsage = 0.8,
        ThresholdDriveUsage = 0.8,
        ThresholdRunningJobs = 16
    };

    [SetUp]
    public void SetUp()
    {
        _jobRunner.Reset();
        _resourcesReader.Reset();
    }

    [Test]
    public async Task CanRunNewJob_ALotOfJobs_ReturnFalse()
    {
        // arrange
        _jobRunner
            .Setup(m => m.RunningJobsCount)
            .Returns(100);

        var monitor = Services.GetRequiredService<ResourcesAnalyzer>();

        // act
        var result = await monitor.CanRunNewJobAsync(default);

        // assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanRunNewJob_HighCpuLoad_ReturnFalse()
    {
        // arrange
        SetupCpuLoad(new(100, 0), new(150, 0));

        var monitor = Services.GetRequiredService<ResourcesAnalyzer>();

        // act
        var result = await monitor.CanRunNewJobAsync(default);

        // assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanRunNewJob_HighRamLoad_ReturnFalse()
    {
        // arrange
        SetupCpuLoad(new(100, 0), new(150, 50));
        SetupRamLoad(new(1000, 100));

        var monitor = Services.GetRequiredService<ResourcesAnalyzer>();

        // act
        var result = await monitor.CanRunNewJobAsync(default);

        // assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanRunNewJob_HighDriveLoad_ReturnFalse()
    {
        // arrange
        SetupCpuLoad(new(100, 0), new(150, 50));
        SetupRamLoad(new(1000, 1000));
        SetupDriveLoad(new(1000, 100));

        var monitor = Services.GetRequiredService<ResourcesAnalyzer>();

        // act
        var result = await monitor.CanRunNewJobAsync(default);

        // assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanRunNewJob_AllFine_ReturnTrue()
    {
        // arrange
        SetupCpuLoad(new(100, 0), new(150, 50));
        SetupRamLoad(new(1000, 1000));
        SetupDriveLoad(new(1000, 1000));

        var monitor = Services.GetRequiredService<ResourcesAnalyzer>();

        // act
        var result = await monitor.CanRunNewJobAsync(default);

        // assert
        Assert.That(result, Is.True);
    }

    private void SetupCpuLoad(CpuStat first, CpuStat second)
    {
        _resourcesReader
            .SetupSequence(m => m.GetCpuStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(first)
            .ReturnsAsync(second);
    }

    private void SetupRamLoad(MemStat memStat)
    {
        _resourcesReader
            .Setup(m => m.GetRamStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(memStat);
    }

    private void SetupDriveLoad(DriveStat driveStat)
    {
        _resourcesReader
            .Setup(m => m.GetDriveStatisticsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(driveStat);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(_jobRunner.Object);
        services.AddSingleton(_resourcesReader.Object);
        services.AddSingleton(_jobEnvironmentOptions);
        services.AddSingleton(_resourceMonitorOptions);
        services.AddTransient<ResourcesAnalyzer>();
    }
}
