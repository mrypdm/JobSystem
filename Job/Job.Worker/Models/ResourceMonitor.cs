namespace Job.Worker.Models;

/// <summary>
/// Monitor for system resources
/// </summary>
public class ResourceMonitor
{
    /// <summary>
    /// Record for memory stats
    /// </summary>
    public record MemStat(long TotalMemory, long AvailableMemory, double Usage);

    /// <summary>
    /// Load average load of CPU in percent
    /// </summary>
    public static async Task<double> GetCpuLoadAsync(CancellationToken cancellationToken)
    {
        var cpuStatTotal = await File.ReadAllLinesAsync("/proc/stat", cancellationToken);
        var cpuStat = cpuStatTotal[0];

        // cpu user nice system idle iowait irq softirq steal guest guest_nice
        var parts = cpuStat
            .Split(' ', StringSplitOptions.TrimEntries)
            .Select(ParseOrDefault)
            .ToArray();

        var idleTime = parts[4];
        var totalTime = parts.Sum();

        if (totalTime == 0)
        {
            throw new InvalidDataException("Total time of CPU is zero");
        }

        var cpuUsage = 1 - (double)idleTime / totalTime;
        return cpuUsage;
    }

    /// <summary>
    /// Get memory load in percent
    /// </summary>
    public static async Task<MemStat> GetMemLoadAsync(CancellationToken cancellationToken)
    {
        var memInfoTotal = await File.ReadAllLinesAsync("/proc/meminfo", cancellationToken);
        var memTotal = ParseOrDefault(memInfoTotal[0].Split(":", StringSplitOptions.TrimEntries)[1]);
        var memAvailable = ParseOrDefault(memInfoTotal[2].Split(":", StringSplitOptions.TrimEntries)[1]);

        if (memTotal == 0)
        {
            throw new InvalidDataException("Total memory is zero");
        }

        var memUsage = 1 - (double)memAvailable / memTotal;
        return new(memTotal, memAvailable, memUsage);
    }

    /// <summary>
    /// Get disk load in percent
    /// </summary>
    public static double GetDiskLoad(string path)
    {
        var drive = new DriveInfo(path);
        var diskUsage = 1 - (double)drive.TotalFreeSpace / drive.TotalSize;
        return diskUsage;
    }

    private static long ParseOrDefault(string str)
    {
        return long.TryParse(str, out var res) ? res : 0;
    }
}
