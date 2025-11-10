namespace Job.WebApi.Client.Exceptions;

/// <summary>
/// Timeout exception of HTTP call to Job.WebApi
/// </summary>
public class JobWebApiTimeoutException(string message, Exception innerException) : Exception(message, innerException);
