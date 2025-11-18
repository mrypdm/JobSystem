using Microsoft.Extensions.Logging;
using Shared.Contract.Options;

namespace Shared.Contract.Logging;

/// <summary>
/// Provider for <see cref="SimpleLogger"/>
/// </summary>
public sealed class SimpleLoggerProvider(SimpleFormatter consoleFormatter, SimpleLoggerOptions options)
    : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopeProvider;

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new SimpleLogger(
            categoryName ?? nameof(SimpleLogger),
            consoleFormatter,
            _scopeProvider,
            options);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // NOP
    }

    /// <inheritdoc />
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }
}
