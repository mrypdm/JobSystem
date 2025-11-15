using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Shared.Database.Migrations;

/// <summary>
/// Base migration for Database
/// </summary>
public abstract class BaseMigration : IDatabaseMigration
{
    /// <inheritdoc />
    public abstract Task ApplyAsync(DbContext context, CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task DiscardAsync(DbContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Do safe Drop SQL
    /// </summary>
    protected static async Task SafeDropSqlAsync(DbContext context, string sql, CancellationToken cancellationToken)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (PostgresException e) when (e.MessageText.Contains("does not exist"))
        {
            // NOP
        }
    }
}
