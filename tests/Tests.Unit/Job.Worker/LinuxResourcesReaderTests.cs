using Job.Worker.Resources.Readers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tests.Common;

namespace Tests.Unit.Job.Worker;
/// <summary>
/// Tests for <see cref="LinuxResourcesReader"/>
/// </summary>
[TestFixture]
internal class LinuxResourcesReaderTests : TestBase
{
    [Test]
    public async Task GetCpuStatistics_ShouldReturnCorrectData()
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
    public async Task GetRamStatistics_ShouldReturnCorrectData()
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
    public async Task GetDriveStatistics_ShouldReturnCorrectData()
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

    /// <inheritdoc />
    protected override void ConfigureServices(HostApplicationBuilder builder)
    {
        base.ConfigureServices(builder);
        builder.Services.AddTransient(_ => new LinuxResourcesReader()
        {
            CpuStatFilePath = Path.Combine("TestData/resource-reader", "proc-stat"),
            RamStatFilePath = Path.Combine("TestData/resource-reader", "meminfo"),
        });
    }
}
