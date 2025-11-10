using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;

namespace User.WebApp.Filters;

/// <summary>
/// Filter for Postgres exceptions
/// </summary>
public class PostgresExceptionsFilter(ILogger<PostgresExceptionsFilter> logger) : ExceptionFilterAttribute
{
    /// <inheritdoc />
    public override void OnException(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExceptionHandled == true)
        {
            return;
        }

        if (context.Exception is PostgresException dbException)
        {
            logger.LogError(dbException, "Postgres exception handled in filter");
            context.ExceptionHandled = true;
            context.Result = new ContentResult()
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Content = "Database is failing",
                ContentType = "text/plain"
            };
        }
    }
}
