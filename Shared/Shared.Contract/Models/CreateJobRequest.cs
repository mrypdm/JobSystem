using System;

namespace Shared.Contract.Models;

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
    /// Steps of Job
    /// </summary>
    public string[] Steps { get; set; }
}
