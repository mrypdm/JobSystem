using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contract.Options;

namespace Shared.Contract.Logging;

/// <summary>
/// Simple logger
/// </summary>
public class SimpleLogger(
    string name,
    SimpleFormatter simpleFormatter,
    IExternalScopeProvider scopeProvider,
    SimpleLoggerOptions options) : ILogger
{
    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return scopeProvider is null ? default! : scopeProvider.Push(state);
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && options.IsEnabled();
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        simpleFormatter.Write(
            new LogEntry<TState>(logLevel, name, eventId, state, exception, formatter),
            scopeProvider,
            logLevel == LogLevel.Error ? options.ErrorOutput() : options.StandardOutput());
    }
}
