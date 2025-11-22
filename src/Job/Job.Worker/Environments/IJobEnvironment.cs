using Job.Worker.Models;

namespace Job.Worker.Environments;

/// <summary>
/// Environment for Job
/// </summary>
public interface IJobEnvironment
{
    /// <summary>
    /// Prepares Job environment
    /// </summary>
    void PrepareEnvironment(RunJobModel jobModel);

    /// <summary>
    /// Clears Job environment
    /// </summary>
    void ClearEnvironment(RunJobModel jobModel);
}
