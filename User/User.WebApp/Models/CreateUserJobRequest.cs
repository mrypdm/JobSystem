using System;

namespace User.WebApp.Models;

/// <summary>
/// Request to create user Job
/// </summary>
public class CreateUserJobRequest
{
    /// <summary>
    /// Timeout of Job
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Steps of Job
    /// </summary>
    public string[] Steps { get; set; }
}
