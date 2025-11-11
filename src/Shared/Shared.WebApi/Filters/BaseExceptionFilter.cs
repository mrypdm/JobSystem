using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Shared.WebApi.Filters;

/// <summary>
/// Filter for exceptions
/// </summary>
public abstract class BaseExceptionFilter(ILogger<BaseExceptionFilter> logger) : IExceptionFilter
{
    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExceptionHandled == true && !IsSupportedException(context.Exception))
        {
            return;
        }

        logger.LogError(context.Exception, "[{ExceptionType}] handled in filter", context.Exception.GetType().Name);
        context.ExceptionHandled = true;
        context.Result = new ContentResult()
        {
            StatusCode = (int)GetStatusCode(context.Exception),
            Content = GetMessage(context.Exception),
            ContentType = "text/plain"
        };
    }

    /// <summary>
    /// If <paramref name="exception"/> is supported by filter
    /// </summary>
    protected abstract bool IsSupportedException(Exception exception);

    /// <summary>
    /// Get status code by exception
    /// </summary>
    protected virtual HttpStatusCode GetStatusCode(Exception exception)
    {
        return HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Get message by exception
    /// </summary>
    protected virtual string GetMessage(Exception exception)
    {
        return exception.Message;
    }
}
