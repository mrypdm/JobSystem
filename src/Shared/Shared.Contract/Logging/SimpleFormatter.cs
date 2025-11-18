using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Shared.Contract.Options;

namespace Shared.Contract.Logging;

/// <summary>
/// Formatter with timestamp and better log level
/// </summary>
public class SimpleFormatter(SimpleFormatterOptions options)
{
    /// <inheritdoc />
    public void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
    {
        if (textWriter is null)
        {
            return;
        }

        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        var exception = logEntry.Exception?.ToString();
        textWriter.WriteLine($"[{GetTimestamp()} {GetLogLevel(logEntry.LogLevel)}] [{logEntry.Category}] {message}");

        if (options.IncludeScopes && scopeProvider is not null)
        {
            scopeProvider.ForEachScope((scope, state) => state.WriteLine($"\t=> {scope}"), textWriter);
        }

        if (exception is not null && !message.Contains(exception))
        {
            textWriter.WriteLine(exception);
        }
    }

    private string GetTimestamp()
    {
        return (options.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now).ToString(options.TimestampFormat);
    }

    private string GetLogLevel(LogLevel logLevel)
    {
        var logLevelStr = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "NON",
        };

        if (!options.WithColors)
        {
            return logLevelStr;
        }

        const string defaultColor = "\e[39m\e[22m";

        var logLevelColor = logLevel switch
        {
            LogLevel.Trace => "\e[1m\e[34m", // Blue
            LogLevel.Debug => "\e[1m\e[36m", // Cyan
            LogLevel.Information => "\e[32m", // DarkGreen
            LogLevel.Warning => "\e[1m\e[33m", // Yellow
            LogLevel.Error => "\e[1m\e[31m", // Red
            LogLevel.Critical => "\e[1m\e[35m", // Magenta
            _ => defaultColor
        };

        return $"{logLevelColor}{logLevelStr}{defaultColor}";
    }
}

/// <summary>
/// Console variant of <see cref="SimpleFormatter"/>
/// </summary>
public class SimpleConsoleFormatter(IOptions<SimpleFormatterOptions> options)
    : ConsoleFormatter(nameof(SimpleConsoleFormatter))
{
    private readonly SimpleFormatter _internalFormatter = new(options.Value);

    /// <inheritdoc />
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
    {
        _internalFormatter.Write(logEntry, scopeProvider, textWriter);
    }
}
