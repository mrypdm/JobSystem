using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using Org.BouncyCastle.X509;
using Shared.Contract.SslOptions;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace Shared.Contract;

/// <summary>
/// Validator for SSL certificates
/// </summary>
public class SslValidator(Pkcs12CertificateOptions options)
{
    private readonly Lazy<X509Chain> _validationChain = new(() =>
    {
        var chain = X509Chain.Create();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Clear();
        chain.ChainPolicy.CustomTrustStore.AddRange(options.CertificateChain);
        return chain;
    });
    private readonly Lazy<HashSet<string>> _revokedCertificates = new(() =>
    {
        var crlParser = new X509CrlParser();
        var crl = crlParser.ReadCrl(File.ReadAllBytes(options.RevocationListFilePath));
        return [.. crl.GetRevokedCertificates().Select(m => Convert.ToHexString(m.SerialNumber.ToByteArray()))];
    });
    private readonly MemoryCache CertificateCache = new(new MemoryCacheOptions()
    {
        ExpirationScanFrequency = TimeSpan.FromMinutes(15)
    });

    /// <summary>
    /// Chain policy for validation
    /// </summary>
    public X509ChainPolicy ChainPolicy => _validationChain.Value.ChainPolicy;

    /// <summary>
    /// Validate certificate
    /// </summary>
    public bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
    {
        return Validate((X509Certificate2)certificate, chain, errors);
    }

    /// <summary>
    /// Validate certificate
    /// </summary>
    public bool Validate(X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
    {
        return CertificateCache.GetOrCreate(certificate.Thumbprint, entry =>
        {
            var result = certificate is not null
                && errors == SslPolicyErrors.None
                && !IsRevoked(certificate)
                && _validationChain.Value.Build(certificate);

            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            entry.Value = result;
            return result;
        });
    }

    /// <summary>
    /// If certeficate revoked
    /// </summary>
    public bool IsRevoked(X509Certificate2 certificate)
    {
        return _revokedCertificates.Value.Contains(certificate.SerialNumber);
    }
}
