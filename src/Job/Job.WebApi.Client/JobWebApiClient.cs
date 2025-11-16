using Flurl.Http;
using Job.Contract;
using Job.WebApi.Client.Exceptions;
using Microsoft.Extensions.Logging;

namespace Job.WebApi.Client;

/// <inheritdoc />
public sealed class JobWebApiClient(
    IFlurlClientFactory factory,
    JobWebApiClientOptions options,
    ILogger<JobWebApiClient> logger) : IJobWebApiClient
{
    private readonly IFlurlClient _httpClient = factory.Create(options);

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <inheritdoc />
    public async Task<Guid> CreateNewJobAsync(CreateJobRequest request, CancellationToken cancellationToken)
    {
        LogHttpRequest("POST", $"/api/jobs");
        return await DoHttpRequest(() => _httpClient
            .Request("api", "jobs")
            .PostJsonAsync(request, cancellationToken: cancellationToken)
            .ReceiveJson<Guid>());
    }

    /// <inheritdoc />
    public async Task<JobResultResponse> GetJobResultsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        LogHttpRequest("GET", $"/api/jobs/{jobId}");
        return await DoHttpRequest(() => _httpClient
            .Request("api", "jobs", jobId.ToString())
            .GetJsonAsync<JobResultResponse>(cancellationToken: cancellationToken));
    }

    private void LogHttpRequest(string method, string path)
    {
        logger.LogDebug("Doing HTTP request [{Method} {Path}]", method, path);
    }

    private static async Task<TResult> DoHttpRequest<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            return await action();
        }
        catch (FlurlHttpTimeoutException e)
        {
            throw new JobWebApiTimeoutException("Call to Job.WebApi timed out", e);
        }
        catch (FlurlHttpException e) when (e.Call.Response is not null)
        {
            var content = await e.Call.Response.GetStringAsync();
            throw new JobWebApiException(e.Call.Response.ResponseMessage.StatusCode, content, e);
        }
        catch (FlurlHttpException e)
        {
            throw new JobWebApiException(null, "Call to Job.WebApi failed", e);
        }
    }
}
