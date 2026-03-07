using Serilog;

namespace Shared.Contract.Extensions;

/// <summary>
/// Extensions for Logging
/// </summary>
public static class LoggingExtensions
{
    private const string CriticalField = "IsCritical";

    /// <summary>
    /// Marks log event as critical
    /// </summary>
    public static ILogger Critical(this ILogger logger)
    {
        return logger.ForContext(CriticalField, true);
    }
}
