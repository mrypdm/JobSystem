using Job.Worker.Resources.Models;

namespace Job.Worker.Resources.Readers;

/// <summary>
/// Reader of Linux system resources
/// </summary>
public class LinuxResourcesReader : IResourcesReader
{
    internal string CpuStatFilePath { get; init; } = "/proc/stat";

    internal string RamStatFilePath { get; init; } = "/proc/meminfo";

    /// <inheritdoc />
    public async Task<CpuStat> GetCpuStatisticsAsync(CancellationToken cancellationToken)
    {
        var cpuStatTotal = await File.ReadAllLinesAsync(CpuStatFilePath, cancellationToken);
        var cpuStat = cpuStatTotal[0];

        // cpu user nice system idle iowait irq softirq steal guest guest_nice
        var parts = cpuStat
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseOrDefault)
            .ToArray();

        return new CpuStat(parts.Sum(), parts[4]);
    }

    /// <inheritdoc />
    public async Task<MemStat> GetRamStatisticsAsync(CancellationToken cancellationToken)
    {
        var memInfoTotal = await File.ReadAllLinesAsync(RamStatFilePath, cancellationToken);
        var memTotal = ParseOrDefault(memInfoTotal[0]
            .Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[1]);
        var memAvailable = ParseOrDefault(memInfoTotal[2]
            .Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[1]);

        if (memTotal == 0)
        {
            throw new InvalidDataException("Total memory is zero");
        }

        return new MemStat(memTotal / 1024, memAvailable / 1024);
    }

    /// <inheritdoc />
    public Task<DriveStat> GetDriveStatisticsAsync(string path, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(path);
        var drive = new DriveInfo(path);
        return Task.FromResult(new DriveStat(drive.TotalSize, drive.AvailableFreeSpace));
    }

    private static long ParseOrDefault(string str)
    {
        return long.TryParse(str, out var res) ? res : 0;
    }
}
