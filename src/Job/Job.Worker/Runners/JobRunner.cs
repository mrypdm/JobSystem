using System.Collections.Concurrent;
using Job.Contract;
using Job.Database.Contexts;
using Job.Worker.Collectors;
using Job.Worker.Environments;
using Job.Worker.JobProcesses;
using Job.Worker.Models;
using Microsoft.Extensions.Logging;
using Shared.Contract.Owned;

namespace Job.Worker.Runners;

/// <inheritdoc />
public class JobRunner(
    IOwnedService<IJobDbContext> jobsDbContextOwned,
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
    }

    /// <inheritdoc />
    public void RunJob(RunJobModel runJobModel)
    {
        ArgumentNullException.ThrowIfNull(runJobModel);

        if (_jobs.ContainsKey(runJobModel.Id))
        {
            logger.LogWarning("Job [{JobId}] is already running", runJobModel.Id);
            return;
        }

        var jobTask = Task.Run(() => RunJobAsync(runJobModel));
        _jobs.TryAdd(runJobModel.Id, jobTask);
    }

    private async Task RunJobAsync(RunJobModel jobModel)
    {
        try
        {
            jobEnvironment.PrepareEnvironment(jobModel);
            await processRunner.RunProcessAsync(jobModel);
            await resultsCollector.CollectResultsAsync(jobModel);

            using var jobsDbContext = jobsDbContextOwned.Value;
            await jobsDbContext.SetJobResultsAsync(jobModel.Id, jobModel.Status, jobModel.Results, default);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while running Job [{JobId}]", jobModel.Id);
            await TrySetFault(jobModel);
        }
        finally
        {
            jobEnvironment.ClearEnvironment(jobModel);
            _jobs.TryRemove(jobModel.Id, out _);
        }
    }

    private async Task TrySetFault(RunJobModel jobModel)
    {
        try
        {
            using var jobsDbContext = jobsDbContextOwned.Value;
            await jobsDbContext.SetJobResultsAsync(jobModel.Id, JobStatus.Fault, [], default);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot set Fault result for Job [{JobId}]", jobModel.Id);
        }
    }
}
