using System.Net;
using Job.WebApi.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace User.WebApp.Filters;

/// <summary>
/// Filter for Job.WebApi exceptions
/// </summary>
public class JobWebApiExceptionsFilter(ILogger<JobWebApiExceptionsFilter> logger) : ExceptionFilterAttribute
{
    /// <inheritdoc />
    public override void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExceptionHandled == true)
        {
            return;
        }

        if (context.Exception is JobWebApiException apiException)
        {
            logger.LogError(apiException, "Job.WebApi exception handled in filter");
            var content = apiException.StatusCode >= HttpStatusCode.InternalServerError
                ? "Job.WebApi is failing"
                : apiException.Message;

            context.ExceptionHandled = true;
            context.Result = new ContentResult()
            {
                StatusCode = (int?)apiException.StatusCode,
                Content = content,
                ContentType = "text/plain"
            };
        }
        else if (context.Exception is JobWebApiTimeoutException timeoutException)
        {
            logger.LogError(timeoutException, "Job.WebApi timeout exception handled in filter");
            context.ExceptionHandled = true;
            context.Result = new ContentResult()
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Content = "Job.WebApi is timeouted",
                ContentType = "text/plain"
            };
        }
    }
}
