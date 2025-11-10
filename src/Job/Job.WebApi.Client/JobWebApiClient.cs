using Flurl.Http;
using Job.Contract;
using Microsoft.Extensions.Logging;

namespace Job.WebApi.Client;

/// <inheritdoc />
public class JobWebApiClient(IFlurlClient httpClient, ILogger<JobWebApiClient> logger) : IJobWebApiClient
{
    /// <inheritdoc />
    public async Task<Guid> CreateNewJobAsync(CreateJobRequest request, CancellationToken cancellationToken)
    {
        LogHttpRequest("POST", $"/api/jobs");
        return await httpClient
            .Request("api", "jobs")
            .PostJsonAsync(request, cancellationToken: cancellationToken)
            .ReceiveJson<Guid>();
    }

    /// <inheritdoc />
    public async Task<JobResultResponse> GetJobResultsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        LogHttpRequest("GET", $"/api/jobs/{jobId}");
        return await httpClient
            .Request("api", "jobs", jobId.ToString())
            .GetJsonAsync<JobResultResponse>(cancellationToken: cancellationToken);
    }

    private void LogHttpRequest(string method, string path)
    {
        logger.LogDebug("Doing HTTP request [{Method} {Path}]", method, path);
    }
}
