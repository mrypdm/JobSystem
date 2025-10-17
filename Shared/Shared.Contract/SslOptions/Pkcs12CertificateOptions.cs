using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Shared.Contract.SslOptions;

/// <summary>
/// Options for PKCS12 certificates
/// </summary>
public class Pkcs12CertificateOptions : CertificateOptions
{
    private readonly Lazy<X509Certificate2Collection> _certificateChain;
    private readonly Lazy<X509Chain> _validationChain;

    /// <summary>
    /// Creates new instance
    /// </summary>
    public Pkcs12CertificateOptions()
    {
        _certificateChain = new(() => X509CertificateLoader.LoadPkcs12CollectionFromFile(CertificateFilePath, Password));
        _validationChain = new(() =>
        {
            var chain = X509Chain.Create();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            chain.ChainPolicy.CustomTrustStore.Clear();
            chain.ChainPolicy.CustomTrustStore.AddRange(Chain);
            return chain;
        });
    }

    /// <summary>
    /// Get whole certificate chain
    /// </summary>
    public X509Certificate2Collection Chain => _certificateChain.Value;

    /// <summary>
    /// Get certificate
    /// </summary>
    public X509Certificate2 Certificate => _certificateChain.Value.First();

    /// <summary>
    /// Get common name (username) from certificate
    /// </summary>
    public string CommonName => Certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

    /// <summary>
    /// Validates certificate with chain
    /// </summary>
    public bool ValidateCertificate(X509Certificate2 certificate)
    {
        return certificate is not null && _validationChain.Value.Build(certificate);
    }
}
