using Job.Worker.Models;

namespace Job.Worker.Processes;

/// <summary>
/// Runner for processes
/// </summary>
public interface IJobProcessRunner
{
    /// <summary>
    /// Run process for Job
    /// </summary>
    Task RunProcessAsync(RunJobModel jobModel);
}
