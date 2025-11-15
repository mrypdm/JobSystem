using Microsoft.EntityFrameworkCore;

namespace Shared.Database.Migrations;

/// <summary>
/// Migration for DataBase
/// </summary>
public interface IDatabaseMigration
{
    /// <summary>
    /// Apply migration
    /// </summary>
    Task ApplyAsync(DbContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Discard migration
    /// </summary>
    Task DiscardAsync(DbContext context, CancellationToken cancellationToken);
}
