using Job.Worker.Options;
using Job.Worker.Resources.Readers;
using Job.Worker.Runners;
using Serilog;
using Shared.Contract.Extensions;

namespace Job.Worker.Resources.Analyzers;

/// <inheritdoc />
public class ResourcesAnalyzer(
    IJobRunner jobRunner,
    IResourcesReader resourcesReader,
    JobEnvironmentOptions jobEnvironmentOptions,
    ResourcesAnalyzerOptions resourceMonitorOptions,
    ILogger logger) : IResourcesAnalyzer
{
    private readonly ILogger _logger = logger.ForContext<ResourcesAnalyzer>();

    /// <inheritdoc />
    public async Task<bool> CanRunNewJobAsync(CancellationToken cancellationToken)
    {
        if (jobRunner.RunningJobsCount > resourceMonitorOptions.ThresholdRunningJobs)
        {
            _logger.Information("Running Jobs count is [{RunningJobs}], cannot run new job",
                jobRunner.RunningJobsCount);
            return false;
        }

        var cpu = await GetCpuLoadAsync(cancellationToken);
        if (cpu > resourceMonitorOptions.ThresholdCpuUsage)
        {
            _logger.Critical().Warning("CPU usage is [{CpuUsage}], cannot run new Job", cpu);
            return false;
        }

        var memory = await resourcesReader.GetRamStatisticsAsync(cancellationToken);
        var memoryUsageOfOneJob = (double)jobEnvironmentOptions.MemoryUsage / memory.Total;
        if (memory.UsagePercentage + memoryUsageOfOneJob > resourceMonitorOptions.ThresholdMemoryUsage)
        {
            _logger.Critical().Warning("Memory usage is [{MemoryUsage}, {EnrichedMemoryUsage}], cannot run new Job",
                memory.UsagePercentage, memory.UsagePercentage + memoryUsageOfOneJob);
            return false;
        }

        var drive = await resourcesReader.GetDriveStatisticsAsync(jobEnvironmentOptions.JobsDirectory,
            cancellationToken);
        if (drive.UsagePercentage > resourceMonitorOptions.ThresholdDriveUsage)
        {
            _logger.Critical().Warning("Drive usage is [{DriveUsage}], cannot run new Job", drive.UsagePercentage);
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
