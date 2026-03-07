using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Contract;
using Shared.Contract.Extensions;
using Shared.Database.Migrations;
using ILogger = Serilog.ILogger;

namespace Shared.Database;

/// <summary>
/// Common context for PostgreSQL databases
/// </summary>
public abstract class PostgreDbContext : DbContext
{
    public PostgreDbContext(DbContextOptions options, ILogger logger) : base(options)
    {
        Logger = logger.ForContext(GetType());
    }

    protected ILogger Logger { get; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pgdbo");
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Migrate Database
    /// </summary>
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        var migrationInterface = typeof(IDatabaseMigration);

        var migrationsTypes = GetType().Assembly.GetTypes()
            .Where(m => !m.IsInterface && !m.IsAbstract && m.IsAssignableTo(migrationInterface));

        foreach (var migrationType in migrationsTypes)
        {
            var migration = Activator.CreateInstance(migrationType) as IDatabaseMigration;
            await migration.ApplyAsync(this, cancellationToken);
            Logger.Critical().Warning("Migration [{MigrationName}] was applied", migration.GetType().Name);
        }
    }

    /// <summary>
    /// Reset Database
    /// </summary>
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        var migrationInterface = typeof(IDatabaseMigration);

        var migrationsTypes = GetType().Assembly.GetTypes()
            .Where(m => !m.IsInterface && !m.IsAbstract && m.IsAssignableTo(migrationInterface));

        foreach (var migrationType in migrationsTypes)
        {
            var migration = Activator.CreateInstance(migrationType) as IDatabaseMigration;
            await migration.DiscardAsync(this, cancellationToken);
            Logger.Critical().Warning("Migration [{MigrationName}] was discarded", migration.GetType().Name);
        }

        await Database.EnsureDeletedAsync(cancellationToken);
        await Database.EnsureCreatedAsync(cancellationToken);
        Logger.Critical().Warning("Database [{DatabaseName}] was recreated", GetType().Name);
    }

    /// <summary>
    /// Build options for <see cref="PostgreDbContext"/>
    /// </summary>
    public static DbContextOptionsBuilder BuildOptions(
        DbContextOptionsBuilder builder,
        DatabaseOptions databaseOptions,
        SslValidator sslValidator,
        ILoggerFactory loggerFactory = null,
        bool forTests = false)
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = databaseOptions.HostName,
            Port = databaseOptions.Port,
            Database = databaseOptions.DatabaseName,
            Username = databaseOptions.CommonName,
            Pooling = !forTests,
        }.ConnectionString;

        builder.UseNpgsql(
            connectionString,
            options => options
                .EnableRetryOnFailure(databaseOptions.RetriesCount, databaseOptions.RetryDelay, errorCodesToAdd: null)
                .ConfigureDataSource(dataSource =>
                {
                    dataSource.UseSslClientAuthenticationOptionsCallback(sslOptions =>
                    {
                        sslOptions.EnabledSslProtocols = SslProtocols.Tls13;
                        sslOptions.CertificateChainPolicy = sslValidator.ChainPolicy;
                        sslOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
                        sslOptions.ClientCertificates = databaseOptions.CertificateChain;
                        sslOptions.RemoteCertificateValidationCallback
                            = (_, cert, chain, error) => sslValidator.Validate((X509Certificate2)cert, chain, error);
                    });
                }));

        if (loggerFactory is not null)
        {
            builder.UseLoggerFactory(loggerFactory);
        }

        if (forTests)
        {
            builder
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging();
        }

        return builder;
    }
}
