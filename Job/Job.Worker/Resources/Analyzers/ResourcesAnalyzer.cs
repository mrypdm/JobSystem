using Job.Worker.Options;
using Job.Worker.Resources.Readers;
using Job.Worker.Runners;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Resources.Analyzers;

/// <inheritdoc />
public partial class ResourcesAnalyzer(
    IJobRunner jobRunner,
    IResourcesReader resourcesReader,
    JobEnvironmentOptions jobEnvironmentOptions,
    ResourcesAnalyzerOptions resourceMonitorOptions,
    ILogger<ResourcesAnalyzer> logger) : IResourcesAnalyzer
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
        if (cpu > resourceMonitorOptions.ThresholdCpuUsage)
        {
            logger.LogCritical("CPU usage is [{CpuUsage}], cannot run new Job", cpu);
            return false;
        }

        var memory = await resourcesReader.GetRamStatisticsAsync(cancellationToken);
        var memoryUsageOfOneJob = jobEnvironmentOptions.MemoryUsage / memory.TotalMemory;
        if (memory.UsagePercetage + memoryUsageOfOneJob > resourceMonitorOptions.ThresholdMemoryUsage)
        {
            logger.LogCritical("Memory usage is [{MemoryUsage}, {EnrichedMemoryUsage}], cannot run new Job",
                memory.UsagePercetage, memory.UsagePercetage + memoryUsageOfOneJob);
            return false;
        }

        var drive = await resourcesReader.GetDriveStatisticsAsync(jobEnvironmentOptions.JobsDirectory,
            cancellationToken);
        if (drive.UsagePercentage > resourceMonitorOptions.ThresholdDriveUsage)
        {
            logger.LogCritical("Drive usage is [{DriveUsage}], cannot run new Job", drive);
            return false;
        }

        return true;
    }

    private async Task<double> GetCpuLoadAsync(CancellationToken cancellationToken)
    {
        var first = await resourcesReader.GetCpuStatisticsAsync(cancellationToken);
        await Task.Delay(500, cancellationToken);
        var second = await resourcesReader.GetCpuStatisticsAsync(cancellationToken);

        var diffIdle = second.Idle - first.Idle;
        var diffTotal = second.Total - first.Total;

        if (diffTotal == 0)
        {
            throw new InvalidDataException("Total time of CPU is zero");
        }

        var cpuUsage = 1 - (double)diffIdle / diffTotal;
        return cpuUsage;
    }
}
