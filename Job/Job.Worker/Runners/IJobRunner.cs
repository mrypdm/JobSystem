using Job.Worker.Models;

namespace Job.Worker.Runners;

/// <summary>
/// Runner of Jobs
/// </summary>
public interface IJobRunner
{
    /// <summary>
    /// Check if we can run new Job
    /// </summary>
    Task<bool> CanRunNewJob(CancellationToken cancellationToken);

    /// <summary>
    /// Waiting for all Jobs to complete
    /// </summary>
    Task WaitForAllJobs();

    /// <summary>
    /// Run Job
    /// </summary>
    void RunJob(RunJobModel runJobModel);
}
