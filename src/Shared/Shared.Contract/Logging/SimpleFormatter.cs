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
        textWriter.WriteLine($"[{GetTimestamp()} {GetLogLevel(logEntry.LogLevel)}] [{logEntry.Category}] {message}");

        if (options.IncludeScopes && scopeProvider is not null)
        {
            scopeProvider.ForEachScope((scope, state) => state.WriteLine($"\t=> {scope}"), textWriter);
        }

        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine($"{logEntry.Exception}");
        }
    }

    private string GetTimestamp()
    {
        return (options.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now).ToString(options.TimestampFormat);
    }

    private string GetLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "NON",
        };
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
