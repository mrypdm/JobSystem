using System;

namespace User.WebApp.Views.Jobs;

/// <summary>
/// Model for Index page
/// </summary>
public class IndexModel(Guid[] userJobs)
{
    /// <summary>
    /// User Jobs
    /// </summary>
    public Guid[] JobIds { get; } = userJobs;
}
