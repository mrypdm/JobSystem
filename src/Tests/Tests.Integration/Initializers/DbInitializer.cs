using Shared.Database;

namespace Tests.Integration.Initializers;

/// <summary>
/// Initializer for Postgres Databases
/// </summary>
public class DbInitializer(PostgreDbContext jobDbContext) : IInitializer
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await jobDbContext.ResetAsync(cancellationToken);
        await jobDbContext.MigrateAsync(cancellationToken);
    }
}
