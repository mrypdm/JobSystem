using Job.WebApi.Client.Exceptions;
using Shared.WebApi.Filters;

namespace User.WebApp.Filters;

/// <summary>
/// Filter for <see cref="JobWebApiTimeoutExceptionFilter"/>
/// </summary>
public class JobWebApiTimeoutExceptionFilter(ILogger<JobWebApiTimeoutExceptionFilter> logger)
    : BaseExceptionFilter(logger)
{
    /// <inheritdoc />
    protected override bool IsSupportedException(Exception exception)
    {
        return exception is JobWebApiTimeoutException;
    }

    /// <inheritdoc />
    protected override string GetMessage(Exception exception)
    {
        return "Job.WebApi is timeouted";
    }
}
