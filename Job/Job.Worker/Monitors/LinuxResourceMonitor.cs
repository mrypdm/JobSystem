using Job.Worker.Options;
using Job.Worker.Runners;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Monitors;

/// <inheritdoc />
public partial class LinuxResourceMonitor(
    JobEnvironmentOptions jobEnvironmentOptions,
    ResourceMonitorOptions resourceMonitorOptions,
    ILogger<LinuxResourceMonitor> logger,
    IJobRunner jobRunner) : IResourceMonitor
{
    /// <inheritdoc />
    public async Task<bool> CanRunNewJobAsync(CancellationToken cancellationToken)
    {
        if (jobRunner.RunningJobsCount > resourceMonitorOptions.ThresholdRunningJobs)
        {
            logger.LogInformation("Running Jobs count is [{RunningJobs}], cannot run new job",
                jobRunner.RunningJobsCount);
            return false;
        }

        var cpu = await GetCpuLoadAsync(cancellationToken);
        var memory = await GetMemLoadAsync(cancellationToken);
        var drive = GetDriveLoad(jobEnvironmentOptions.JobsDirectory);
        var memoryUsageOfOneJob = jobEnvironmentOptions.MemoryUsage / memory.TotalMemory;

        if (cpu > resourceMonitorOptions.ThresholdCpuUsage)
        {
            logger.LogCritical("CPU usage is [{CpuUsage}], cannot run new Job", cpu);
            return false;
        }

        if (memory.Usage + memoryUsageOfOneJob > resourceMonitorOptions.ThresholdMemoryUsage)
        {
            logger.LogCritical("Memory usage is [{MemoryUsage}, {EnrichedMemoryUsage}], cannot run new Job",
                memory.Usage, memory.Usage + memoryUsageOfOneJob);
            return false;
        }

        if (drive > resourceMonitorOptions.ThresholdDriveUsage)
        {
            logger.LogCritical("Drive usage is [{DriveUsage}], cannot run new Job", drive);
            return false;
        }

        return true;
    }

    private static async Task<double> GetCpuLoadAsync(CancellationToken cancellationToken)
    {
        var first = await GetCurrentCpuStateAsync(cancellationToken);
        await Task.Delay(500, cancellationToken);
        var second = await GetCurrentCpuStateAsync(cancellationToken);

        var diffIdle = second.Idle - first.Idle;
        var diffTotal = second.Total - first.Total;

        if (diffTotal == 0)
        {
            throw new InvalidDataException("Total time of CPU is zero");
        }

        var cpuUsage = 1 - (double)diffIdle / diffTotal;
        return cpuUsage;
    }

    private static async Task<MemStat> GetMemLoadAsync(CancellationToken cancellationToken)
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

    private static double GetDriveLoad(string path)
    {
        var drive = new DriveInfo(path);
        var driveUsage = 1 - (double)drive.TotalFreeSpace / drive.TotalSize;
        return driveUsage;
    }

    private static async Task<(long Idle, long Total)> GetCurrentCpuStateAsync(CancellationToken cancellationToken)
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
        return (idleTime, totalTime);
    }

    private static long ParseOrDefault(string str)
    {
        return long.TryParse(str, out var res) ? res : 0;
    }
}
