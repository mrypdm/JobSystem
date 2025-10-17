using System;
using Shared.Contract.Models;

namespace Job.Worker.Models;

/// <summary>
/// Model of running Job
/// </summary>
public class RunJobModel
{
    /// <summary>
    /// Job Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Job timeout
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Job directory
    /// </summary>
    public string Directory { get; set; }

    /// <summary>
    /// Status of Job
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Datetime when Job was finished
    /// </summary>
    public DateTimeOffset FinishedAt { get; set; }

    /// <summary>
    /// Script to run
    /// </summary>
    public string Script { get; set; }

    /// <summary>
    /// Results of Job
    /// </summary>
    public byte[] Results { get; set; }
}
