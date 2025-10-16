using System;
using Shared.Contract.Models;

namespace Job.Database.Models;

/// <summary>
/// Model of Job
/// </summary>
public class JobDbModel
{
    /// <summary>
    /// Job Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Job Status
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Datetime when Job was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Datetime when Job was started
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Datetime when Job was finished
    /// </summary>
    public DateTimeOffset FinishedAt { get; set; }

    /// <summary>
    /// Timeout of Job
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Steps of Job
    /// </summary>
    public string[] Steps { get; set; }

    /// <summary>
    /// Results of Job
    /// </summary>
    public byte[] Result { get; set; }
}
