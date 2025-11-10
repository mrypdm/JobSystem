using System.Net;
using Job.WebApi.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace User.WebApp.Filters;

/// <summary>
/// Filter for Job.WebApi exceptions
/// </summary>
public class JobWebApiExceptionsFilter : ExceptionFilterAttribute
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
            var content = apiException.StatusCode >= System.Net.HttpStatusCode.InternalServerError
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
