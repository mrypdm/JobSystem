using System;

namespace User.WebApp.Views.Jobs;

/// <summary>
/// Model for Job results
/// </summary>
public class JobResultsModel(Guid jobId)
{
    /// <summary>
    /// Id of Job
    /// </summary>
    public Guid JobId { get; } = jobId;
}
