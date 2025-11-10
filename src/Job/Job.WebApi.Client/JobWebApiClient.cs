using Flurl.Http;
using Job.Contract;
using Job.WebApi.Client.Exceptions;
using Microsoft.Extensions.Logging;

namespace Job.WebApi.Client;

/// <inheritdoc />
public sealed class JobWebApiClient(IFlurlClient httpClient, ILogger<JobWebApiClient> logger, bool ownedClient = false)
    : IJobWebApiClient
{
    /// <inheritdoc />
    public void Dispose()
    {
        if (ownedClient)
        {
            httpClient.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<Guid> CreateNewJobAsync(CreateJobRequest request, CancellationToken cancellationToken)
    {
        LogHttpRequest("POST", $"/api/jobs");
        return await DoHttpRequest(() => httpClient
            .Request("api", "jobs")
            .PostJsonAsync(request, cancellationToken: cancellationToken)
            .ReceiveJson<Guid>());
    }

    /// <inheritdoc />
    public async Task<JobResultResponse> GetJobResultsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        LogHttpRequest("GET", $"/api/jobs/{jobId}");
        return await DoHttpRequest(() => httpClient
            .Request("api", "jobs", jobId.ToString())
            .GetJsonAsync<JobResultResponse>(cancellationToken: cancellationToken));
    }

    private void LogHttpRequest(string method, string path)
    {
        logger.LogDebug("Doing HTTP request [{Method} {Path}]", method, path);
    }

    private async Task<TResult> DoHttpRequest<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            return await action();
        }
        catch (FlurlHttpTimeoutException e)
        {
            throw new JobWebApiTimeoutException("Call to Job.WebApi has timed out", e);
        }
        catch (FlurlHttpException e)
        {
            var content = await e.Call.Response?.GetStringAsync();
            throw new JobWebApiException(e.Call.Response?.ResponseMessage.StatusCode, content, e);
        }
    }
}
