using Job.Worker.Resources.Models;

namespace Job.Worker.Resources.Readers;

/// <summary>
/// Reader of Linux system resources
/// </summary>
public class LinuxResourcesReader : IResourcesReader
{
    /// <inheritdoc />
    public async Task<CpuStat> GetCpuStatisticsAsync(CancellationToken cancellationToken)
    {
        var cpuStatTotal = await File.ReadAllLinesAsync("/proc/stat", cancellationToken);
        var cpuStat = cpuStatTotal[0];

        // cpu user nice system idle iowait irq softirq steal guest guest_nice
        var parts = cpuStat
            .Split(' ', StringSplitOptions.TrimEntries)
            .Select(ParseOrDefault)
            .ToArray();

        return new CpuStat(parts.Sum(), parts[4]);
    }

    /// <inheritdoc />
    public async Task<MemStat> GetRamStatisticsAsync(CancellationToken cancellationToken)
    {
        var memInfoTotal = await File.ReadAllLinesAsync("/proc/meminfo", cancellationToken);
        var memTotal = ParseOrDefault(memInfoTotal[0].Split(":", StringSplitOptions.TrimEntries)[1]);
        var memAvailable = ParseOrDefault(memInfoTotal[2].Split(":", StringSplitOptions.TrimEntries)[1]);

        if (memTotal == 0)
        {
            throw new InvalidDataException("Total memory is zero");
        }

        return new MemStat(memTotal, memAvailable);
    }

    /// <inheritdoc />
    public Task<DriveStat> GetDriveStatisticsAsync(string path, CancellationToken cancellationToken)
    {
        var drive = new DriveInfo(path);
        return Task.FromResult(new DriveStat(drive.TotalSize, drive.AvailableFreeSpace));
    }

    private static long ParseOrDefault(string str)
    {
        return long.TryParse(str, out var res) ? res : 0;
    }
}
