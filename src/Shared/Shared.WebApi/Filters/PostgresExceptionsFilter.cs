using Npgsql;
using Serilog;

namespace Shared.WebApi.Filters;

/// <summary>
/// Filter for <see cref="PostgresException"/>
/// </summary>
public class PostgresExceptionsFilter(ILogger logger) : BaseExceptionFilter(logger)
{
    /// <inheritdoc />
    protected override bool IsSupportedException(Exception exception)
    {
        return exception is PostgresException;
    }

    /// <inheritdoc />
    protected override string GetMessage(Exception exception)
    {
        return "Database is failing";
    }
}
