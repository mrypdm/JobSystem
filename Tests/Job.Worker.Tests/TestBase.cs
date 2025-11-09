using Microsoft.Extensions.Logging;

namespace Job.Worker.Tests;

/// <summary>
/// Base class for tests
/// </summary>
[TestFixture]
internal abstract class TestBase
{
    private static ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole().AddNUnit();
        builder.SetMinimumLevel(LogLevel.Trace);
    });

    /// <summary>
    /// Create logger for <typeparamref name="TClass"/>
    /// </summary>
    protected static ILogger<TClass> CreateLogger<TClass>()
    {
        return new Logger<TClass>(_loggerFactory);
    }
}
