using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using Org.BouncyCastle.X509;
using Shared.Contract.SslOptions;

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
        _revokedCertificates = GetRevokedCertificates(_validationChain, options.RevocationListFilePath);
    }

    /// <summary>
    /// Chain policy for validation
    /// </summary>
    public X509ChainPolicy ChainPolicy => _validationChain.ChainPolicy;

    /// <summary>
    /// Validate certificate
    /// </summary>
    public bool Validate(X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
    {
        if (certificate is null)
        {
            return false;
        }

        return CertificateCache.GetOrCreate(certificate.Thumbprint, entry =>
        {
            // allow untrusted root because we have our custom root loaded to application
            var externalValidation = errors == SslPolicyErrors.None || IsUntrustedRootError(errors, chain);
            var isValid = externalValidation
                && !IsRevoked(certificate)
                && _validationChain.Build(certificate);

            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            entry.Value = isValid;
            return isValid;
        });
    }

    /// <summary>
    /// If certeficate revoked
    /// </summary>
    public bool IsRevoked(X509Certificate2 certificate)
    {
        return certificate is null || _revokedCertificates.Contains(certificate.SerialNumber);
    }

    private static bool IsUntrustedRootError(SslPolicyErrors errors, X509Chain chain)
    {
        if (errors != SslPolicyErrors.RemoteCertificateChainErrors || chain.ChainStatus.Length != 1)
        {
            return false;
        }

        var rootValidationError = chain.ChainStatus[0];

        // windows way
        if (rootValidationError.Status == X509ChainStatusFlags.UntrustedRoot
            && rootValidationError.StatusInformation == "A certificate chain processed, but terminated in a root certificate which is not trusted by the trust provider.")
        {
            return true;
        }

        // linux way
        if (rootValidationError.Status == X509ChainStatusFlags.PartialChain
            && rootValidationError.StatusInformation == "unable to get local issuer certificate")
        {
            return true;
        }

        return false;
    }

    private static HashSet<string> GetRevokedCertificates(X509Chain chain, string crlPath)
    {
        var rootPublicKey = new X509CertificateParser()
            .ReadCertificate(chain.ChainPolicy.CustomTrustStore.Last().GetRawCertData())
            .GetPublicKey();

        var crl = new X509CrlParser().ReadCrl(File.ReadAllBytes(crlPath));
        crl.Verify(rootPublicKey);
        if (crl.NextUpdate < DateTime.Now)
        {
            throw new ArgumentException($"CRL is expired. It was supposed to be updated on {crl.NextUpdate}");
        }

        return crl.GetRevokedCertificates()
            ?.Select(m => Convert.ToHexString(m.SerialNumber.ToByteArray()))
            ?.ToHashSet() ?? [];
    }
}
