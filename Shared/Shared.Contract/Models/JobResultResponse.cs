using System;

namespace Shared.Contract.Models;

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
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Datetime when job was finished
    /// </summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    /// Results of Job
    /// </summary>
    public byte[] Results { get; set; }
}
