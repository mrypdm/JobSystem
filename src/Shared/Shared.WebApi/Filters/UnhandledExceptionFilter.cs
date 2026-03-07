using Serilog;

namespace Shared.WebApi.Filters;

/// <summary>
/// Filter for all exceptions
/// </summary>
public class UnhandledExceptionFilter(ILogger logger) : BaseExceptionFilter(logger)
{
    /// <inheritdoc />
    protected override bool IsSupportedException(Exception exception)
    {
        return true;
    }

    /// <inheritdoc />
    protected override string GetMessage(Exception exception)
    {
        return "Internal error happend";
    }
}
