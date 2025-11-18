using Microsoft.Extensions.Logging.Console;
using Shared.Contract.Logging;

namespace Shared.Contract.Options;

/// <summary>
/// Options fro <see cref="SimpleFormatter"/>
/// </summary>
public class SimpleFormatterOptions : ConsoleFormatterOptions
{
    public SimpleFormatterOptions()
    {
        IncludeScopes = true;
        UseUtcTimestamp = false;
        TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff K";
    }
}
