using System;
using Microsoft.AspNetCore.Http;

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
    /// Script of Job
    /// </summary>
    public IFormFile Script { get; set; }
}
