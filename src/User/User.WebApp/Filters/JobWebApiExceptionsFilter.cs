using System.Net;
using Job.WebApi.Client.Exceptions;
using Shared.WebApi.Filters;

namespace User.WebApp.Filters;

/// <summary>
/// Filter for <see cref="JobWebApiException"/>
/// </summary>
public class JobWebApiExceptionsFilter(ILogger<JobWebApiExceptionsFilter> logger) : BaseExceptionFilter(logger)
{
    /// <inheritdoc />
    protected override bool IsSupportedException(Exception exception)
    {
        return exception is JobWebApiException;
    }

    /// <inheritdoc />
    protected override HttpStatusCode GetStatusCode(Exception exception)
    {
        var jobWebApiException = exception as JobWebApiException;
        return jobWebApiException.StatusCode ?? HttpStatusCode.InternalServerError;
    }

    /// <inheritdoc />
    protected override string GetMessage(Exception exception)
    {
        return GetStatusCode(exception) >= HttpStatusCode.InternalServerError
            ? "Job.WebApi is failing"
            : exception.Message;
    }
}
