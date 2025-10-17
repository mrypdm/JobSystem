using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

        var a = new NpgsqlDataSourceBuilder(connectionString).UseSslClientAuthenticationOptionsCallback(opt =>
        {
            opt.EnabledSslProtocols = SslProtocols.Tls13;
            opt.TargetHost = $"{databaseOptions.HostName}:{databaseOptions.Port}";
            opt.CertificateChainPolicy = new X509ChainPolicy();
            opt.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
            opt.CertificateChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            opt.CertificateChainPolicy.CustomTrustStore.AddRange(databaseOptions.Chain);
            opt.ClientCertificates = [databaseOptions.Certificate];
            opt.RemoteCertificateValidationCallback = (_, cert, _, _) =>
            {
                return databaseOptions.ValidateCertificate(new X509Certificate2(cert));
            };
        });

        builder.UseNpgsql(a.Build());
    }
}
