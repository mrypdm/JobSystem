namespace Job.Contract;

/// <summary>
/// Status of Job
/// </summary>
public enum JobStatus
{
    New = 0,
    Running = 1,
    Finished = 2,
    Timeout = 3,
    Fault = 4,
    Lost = 5,
}
