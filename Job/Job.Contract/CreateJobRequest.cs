using System;

namespace Job.Contract;

/// <summary>
/// Request for creating new Job
/// </summary>
public class CreateJobRequest
{
    /// <summary>
    /// Job Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timeout of Job
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Script of Job
    /// </summary>
    public string Script { get; set; }
}
