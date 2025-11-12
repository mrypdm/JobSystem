using Job.Worker.Resources.Readers;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;

namespace Job.Worker.UnitTests;

/// <summary>
/// Tests for <see cref="LinuxResourcesReader"/>
/// </summary>
[TestFixture]
internal class LinuxResourcesReaderTests : TestBase
{
    [Test]
    public async Task ReadCpuStatistics()
    {
        // arrange
        var reader = Services.GetRequiredService<LinuxResourcesReader>();

        // act
        var cpuStat = await reader.GetCpuStatisticsAsync(default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(cpuStat.Idle, Is.EqualTo(2884481));
        Assert.That(cpuStat.Total, Is.EqualTo(2892443));
    }

    [Test]
    public async Task ReadRamStatistics()
    {
        // arrange
        var reader = Services.GetRequiredService<LinuxResourcesReader>();

        // act
        var cpuStat = await reader.GetRamStatisticsAsync(default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(cpuStat.Available, Is.EqualTo(7448));
        Assert.That(cpuStat.Total, Is.EqualTo(7846));
    }

    [Test]
    public async Task ReadDriveStatistics()
    {
        // arrange
        var driveInfo = new DriveInfo(Environment.CurrentDirectory);
        var reader = Services.GetRequiredService<LinuxResourcesReader>();

        // act
        var cpuStat = await reader.GetDriveStatisticsAsync(Environment.CurrentDirectory, default);

        // assert
        using var _ = Assert.EnterMultipleScope();
        Assert.That(cpuStat.Free, Is.EqualTo(driveInfo.AvailableFreeSpace));
        Assert.That(cpuStat.Total, Is.EqualTo(driveInfo.TotalSize));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddTransient(_ => new LinuxResourcesReader()
        {
            CpuStatFilePath = Path.Combine("TestData", "proc-stat"),
            RamStatFilePath = Path.Combine("TestData", "meminfo"),
        });
    }
}
