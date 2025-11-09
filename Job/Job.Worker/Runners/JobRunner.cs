using System.Collections.Concurrent;
using Job.Database.Contexts;
using Job.Worker.Collectors;
using Job.Worker.Environments;
using Job.Worker.JobProcesses;
using Job.Worker.Models;
using Microsoft.Extensions.Logging;

namespace Job.Worker.Runners;

/// <inheritdoc />
public class JobRunner(
    IJobDbContext jobsDbContext,
    IJobEnvironment jobEnvironment,
    IJobProcessRunner processRunner,
    IResultsCollector resultsCollector,
    ILogger<JobRunner> logger) : IJobRunner
{
    private readonly ConcurrentDictionary<Guid, Task> _jobs = [];

    /// <inheritdoc />
    public long RunningJobsCount => _jobs.Count;

    /// <inheritdoc />
    public async Task WaitForAllJobs()
    {
        await Task.WhenAll(_jobs.Values);
        _jobs.Clear();
    }

    /// <inheritdoc />
    public void RunJob(RunJobModel runJobModel)
    {
        if (_jobs.ContainsKey(runJobModel.Id))
        {
            logger.LogWarning("Job [{JobId}] is already running", runJobModel.Id);
            return;
        }

        var jobTask = Task.Run(() => RunJobAsync(runJobModel));
        _jobs.TryAdd(runJobModel.Id, jobTask);
    }

    private async Task RunJobAsync(RunJobModel runJobModel)
    {
        try
        {
            jobEnvironment.PrepareEnvironment(runJobModel);
            await processRunner.RunProcessAsync(runJobModel);
            await resultsCollector.CollectResults(runJobModel);
            await jobsDbContext.SetJobResultsAsync(runJobModel.Id, runJobModel.Status, runJobModel.Results, default);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while running Job [{JobId}]", runJobModel.Id);
        }
        finally
        {
            jobEnvironment.ClearEnvironment(runJobModel);
            _jobs.TryRemove(runJobModel.Id, out _);
        }
    }
}
