namespace Shared.Contract.Models;

/// <summary>
/// Status of Job
/// </summary>
public enum JobStatus
{
    New = 0,
    Running = 1,
    Finished = 2,
    Timeout = 3,
    Lost = 4
}
