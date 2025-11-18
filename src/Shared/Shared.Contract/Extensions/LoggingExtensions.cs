using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shared.Contract.Logging;
using Shared.Contract.Options;

namespace Shared.Contract.Extensions;

/// <summary>
/// Extensions for Logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Add property with <paramref name="name"/> and <paramref name="value"/> to logging scope
    /// </summary>
    public static IDisposable PushProperty<TValue>(this ILogger logger, string name, TValue value)
    {
        return logger.BeginScope($"{name}: {{{name}}}", value);
    }

    /// <summary>
    /// Add simple logger provider
    /// </summary>
    public static ILoggingBuilder AddSimpleLogger(
        this ILoggingBuilder builder,
        Action<SimpleLoggerOptions> configureLogger = null,
        Action<SimpleFormatterOptions> configureFormatter = null)
    {
        var loggerOptions = new SimpleLoggerOptions();
        configureLogger?.Invoke(loggerOptions);
        builder.Services.AddSingleton(loggerOptions);

        var formatterOptions = new SimpleFormatterOptions();
        configureFormatter?.Invoke(formatterOptions);
        builder.Services.AddSingleton(formatterOptions);

        builder.Services.AddSingleton<SimpleFormatter>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SimpleLoggerProvider>());
        return builder;
    }
}
