using Confluent.Kafka;
using Serilog;
using Serilog.Events;
using Shared.Contract.Extensions;

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

            logger.Write(
                logMessage.LevelAsSerilog(),
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
            logger.Critical().Write(
                error.IsFatal ? LogEventLevel.Fatal : LogEventLevel.Error,
                $"{name} error detected [{{Code}} | {{Reason}}]",
                error.Code, error.Reason);
        };
    }

    private static LogEventLevel LevelAsSerilog(this LogMessage message)
    {
        return message.Level switch
        {
            SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogEventLevel.Fatal,
            SyslogLevel.Error => LogEventLevel.Error,
            SyslogLevel.Warning => LogEventLevel.Warning,
            SyslogLevel.Info or SyslogLevel.Notice => LogEventLevel.Information,
            SyslogLevel.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Verbose
        };
    }

}
