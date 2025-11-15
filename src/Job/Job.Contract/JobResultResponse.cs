namespace Job.Contract;

/// <summary>
/// Results of job
/// </summary>
public class JobResultResponse
{
    /// <summary>
    /// Status of Job
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Datetime when job was started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Datetime when job was finished
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Results of Job
    /// </summary>
    public byte[] Results { get; set; }
}
