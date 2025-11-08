using Job.Contract;

namespace Job.Database.Contexts;

/// <summary>
/// Context for jobs
/// </summary>
public interface IJobDbContext : IDisposable
{
    /// <summary>
    /// Add new Job to database
    /// </summary>
    Task AddNewJobAsync(CreateJobRequest job, CancellationToken cancellationToken);

    /// <summary>
    /// Get Job for running
    /// </summary>
    Task<CreateJobRequest> GetNewJobAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Set Job as running
    /// </summary>
    Task SetJobRunningAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Set Job results
    /// </summary>
    Task SetJobResultsAsync(Guid jobId, JobStatus jobStatus, byte[] results, CancellationToken cancellationToken);

    /// <summary>
    /// Mark Jobs with timeout vialoation as lost
    /// </summary>
    Task MarkLostJobsAsync(TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Get Job results
    /// </summary>
    Task<JobResultResponse> GetJobResults(Guid jobId, CancellationToken cancellationToken);
}
