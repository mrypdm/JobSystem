using Job.Worker.Resources.Readers;

namespace Job.Worker.Tests;

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
        var reader = CreateReader();

        // act
        var cpuStat = await reader.GetCpuStatisticsAsync(default);

        // assert
        Assert.That(cpuStat.Idle, Is.EqualTo(2884481));
        Assert.That(cpuStat.Total, Is.EqualTo(2892443));
    }

    [Test]
    public async Task ReadRamStatistics()
    {
        // arrange
        var reader = CreateReader();

        // act
        var cpuStat = await reader.GetRamStatisticsAsync(default);

        // assert
        Assert.That(cpuStat.Available, Is.EqualTo(7448));
        Assert.That(cpuStat.Total, Is.EqualTo(7846));
    }

    [Test]
    public async Task ReadDriveStatistics()
    {
        // arrange
        var driveInfo = new DriveInfo(Environment.CurrentDirectory);
        var reader = CreateReader();

        // act
        var cpuStat = await reader.GetDriveStatisticsAsync(Environment.CurrentDirectory, default);

        // assert
        Assert.That(cpuStat.Free, Is.EqualTo(driveInfo.AvailableFreeSpace));
        Assert.That(cpuStat.Total, Is.EqualTo(driveInfo.TotalSize));
    }

    private LinuxResourcesReader CreateReader()
    {
        return new LinuxResourcesReader()
        {
            CpuStatFilePath = Path.Combine(Environment.CurrentDirectory, "TestData", "proc-stat"),
            RamStatFilePath = Path.Combine(Environment.CurrentDirectory, "TestData", "meminfo"),
        };
    }
}
