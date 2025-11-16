using Shared.Contract;

namespace Shared.Database.Migrations;

/// <summary>
/// Initializer for Postgres Databases
/// </summary>
public class DbInitializer(PostgreDbContext dbContext) : IInitializer
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.ResetAsync(cancellationToken);
        await dbContext.MigrateAsync(cancellationToken);
    }
}
