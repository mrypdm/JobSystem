using Job.Contract;

namespace Job.WebApi.Client;

/// <summary>
/// Client for Job.WebApi
/// </summary>
public interface IJobWebApiClient
{
    /// <summary>
    /// Create new job
    /// </summary>
    Task<Guid> CreateNewJobAsync(CreateJobRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get job results
    /// </summary>
    Task<JobResultResponse> GetJobResultsAsync(Guid jobId, CancellationToken cancellationToken);
}
