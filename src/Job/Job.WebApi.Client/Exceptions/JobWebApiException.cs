using System.Net;

namespace Job.WebApi.Client.Exceptions;

/// <summary>
/// Exception of HTTP call to Job.WebApi
/// </summary>
public class JobWebApiException(HttpStatusCode? statusCode, string message, Exception innerException)
    : Exception(message, innerException)
{
    /// <summary>
    /// Status code of error response
    /// </summary>
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
