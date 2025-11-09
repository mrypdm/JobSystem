using Job.Worker.Models;

namespace Job.Worker.Runners;

/// <summary>
/// Runner of Jobs
/// </summary>
public interface IJobRunner
{
    /// <summary>
    /// Count of running Jobs
    /// </summary>
    long RunningJobsCount { get; }

    /// <summary>
    /// Waiting for all Jobs to complete
    /// </summary>
    Task WaitForAllJobs();

    /// <summary>
    /// Run Job
    /// </summary>
    void RunJob(RunJobModel runJobModel);
}
