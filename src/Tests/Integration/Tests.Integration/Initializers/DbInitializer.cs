using Microsoft.EntityFrameworkCore;

namespace Tests.Integration.Initializers;

/// <summary>
/// Initializer for databases
/// </summary>
internal class DbInitializer(DbContext jobDbContext) : BaseInitializer
{
    /// <inheritdoc />
    protected override async Task InitializeInternalAsync(CancellationToken cancellationToken)
    {
        await jobDbContext.Database.EnsureDeletedAsync(cancellationToken);
        await jobDbContext.Database.EnsureCreatedAsync(cancellationToken);
        await jobDbContext.Database.MigrateAsync(cancellationToken);
    }
}
