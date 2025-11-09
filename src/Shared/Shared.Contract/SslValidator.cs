using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using Org.BouncyCastle.X509;
using Shared.Contract.SslOptions;
using SystemX509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace Shared.Contract;

/// <summary>
/// Validator for SSL certificates
/// </summary>
public class SslValidator
{
    private readonly X509Chain _validationChain;
    private readonly HashSet<string> _revokedCertificates;
    private readonly MemoryCache CertificateCache = new(new MemoryCacheOptions()
    {
        ExpirationScanFrequency = TimeSpan.FromMinutes(15)
    });

    public SslValidator(Pkcs12CertificateOptions options)
    {
        _validationChain = X509Chain.Create();
        _validationChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        _validationChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        _validationChain.ChainPolicy.CustomTrustStore.Clear();
        _validationChain.ChainPolicy.CustomTrustStore.AddRange(options.CertificateChain);

        var rootPublicKey = new X509CertificateParser()
            .ReadCertificate(_validationChain.ChainPolicy.CustomTrustStore.Last().GetRawCertData())
            .GetPublicKey();
        var crl = new X509CrlParser()
            .ReadCrl(File.ReadAllBytes(options.RevocationListFilePath));
        crl.IsSignatureValid(rootPublicKey);
        _revokedCertificates = [.. crl.GetRevokedCertificates()?.Select(m => Convert.ToHexString(m.SerialNumber.ToByteArray())) ?? []];
    }

    /// <summary>
    /// Chain policy for validation
    /// </summary>
    public X509ChainPolicy ChainPolicy => _validationChain.ChainPolicy;

    /// <summary>
    /// Validate certificate
    /// </summary>
    public bool Validate(object sender, SystemX509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
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
                && _validationChain.Build(certificate);

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
        return _revokedCertificates.Contains(certificate.SerialNumber);
    }
}
