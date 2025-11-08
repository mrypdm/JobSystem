using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Contract;

namespace Shared.Database;

/// <summary>
/// Common context for PostgreSQL databases
/// </summary>
public abstract class PostgreDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Build options for <see cref="PostgreDbContext"/>
    /// </summary>
    public static void BuildOptions(DbContextOptionsBuilder builder, DatabaseOptions databaseOptions)
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = databaseOptions.HostName,
            Port = databaseOptions.Port,
            Database = databaseOptions.DatabaseName,
            Username = databaseOptions.CommonName
        }.ConnectionString;

        var validator = new SslValidator(databaseOptions);

        builder.UseNpgsql(
            connectionString,
            options => options
                .EnableRetryOnFailure(databaseOptions.RetriesCount, databaseOptions.RetryDelay, errorCodesToAdd: null)
                .ConfigureDataSource(dataSource =>
                {
                    dataSource.UseSslClientAuthenticationOptionsCallback(sslOptions =>
                    {
                        sslOptions.EnabledSslProtocols = SslProtocols.Tls13;
                        sslOptions.CertificateChainPolicy = validator.ChainPolicy;
                        sslOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
                        sslOptions.ClientCertificates = databaseOptions.CertificateChain;
                        sslOptions.RemoteCertificateValidationCallback = validator.Validate;
                    });
                }));
    }
}
