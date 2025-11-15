using Microsoft.EntityFrameworkCore;

namespace Tests.Common.Initializers;

/// <summary>
/// Initializer for databases
/// </summary>
public class DbInitializer(DbContext jobDbContext) : IInitializer
{
    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await jobDbContext.Database.EnsureDeletedAsync(cancellationToken);
        await jobDbContext.Database.EnsureCreatedAsync(cancellationToken);
        await jobDbContext.Database.MigrateAsync(cancellationToken);
    }
}
