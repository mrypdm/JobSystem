using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Shared.Broker.Helpers;

/// <summary>
/// Helpers for Broker
/// </summary>
public static class BrokerHelpers
{
    /// <summary>
    /// Get default log handler
    /// </summary>
    public static Action<TType, LogMessage> GetLogHandler<TType>(this ILogger logger, string name)
    {
        return (_, logMessage) =>
        {
            if (logMessage.Level < SyslogLevel.Warning)
            {
                return; // will be logged in ErrorHandler
            }

            logger.Log(
                (LogLevel)logMessage.LevelAs(LogLevelType.MicrosoftExtensionsLogging),
                $"{{{name}Message}}", logMessage.Message);
        };
    }

    /// <summary>
    /// Get default error handler
    /// </summary>
    public static Action<TType, Error> GetErrorHandler<TType>(this ILogger logger, string name)
    {
        return (_, error) =>
        {
            logger.Log(
                error.IsFatal ? LogLevel.Critical : LogLevel.Error,
                $"{name} error detected [{{Code}} | {{Reason}}]",
                error.Code, error.Reason);
        };
    }
}
