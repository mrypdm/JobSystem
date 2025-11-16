using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Org.BouncyCastle.Security;
using Shared.Contract;
using Shared.Contract.SslOptions;
using Tests.Common;

namespace Tests.Unit.Shared.Contract;

/// <summary>
/// Tests for <see cref="SslValidator"/>
/// </summary>
[TestFixture]
public class SslValidatorTests : TestBase
{
    private const string Password = "qwerty-123";

    private const string WrongChainCrlPath = "TestData/ssl-validator/crl.wrong-chain.pem";
    private const string CrlPath = "TestData/ssl-validator/crl.pem";

    private const string DefaultCertificatePath = "TestData/ssl-validator/svc_testhost.keystore.p12";
    private const string RevokedCertificatePath = "TestData/ssl-validator/svc_testhost@revoked.keystore.p12";
    private const string WrongChainCertificatePath = "TestData/ssl-validator/svc_testhost@wrong-chain.keystore.p12";

    [Test]
    public void Init_WrongCrlSign_Throw()
    {
        // arrange & act & assert
        var exception = Assert.Throws<InvalidKeyException>(() => CreateValidator(crlPath: WrongChainCrlPath));
        Assert.That(exception.Message, Does.Contain("CRL does not verify with supplied public key"));
    }

    [Test]
    [TestCase(null, true)]
    [TestCase(RevokedCertificatePath, true)]
    [TestCase(DefaultCertificatePath, false)]
    public void IsRevoked_WithGivenCert_ShouldReturnExpected(string certPath, bool expected)
    {
        // arrange
        var validator = CreateValidator();
        var certToTest = GetCertificate(certPath);

        // act
        var result = validator.IsRevoked(certToTest);

        // assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase(null, SslPolicyErrors.None, false)]
    [TestCase(DefaultCertificatePath, SslPolicyErrors.RemoteCertificateNameMismatch, false)]
    [TestCase(RevokedCertificatePath, SslPolicyErrors.None, false)]
    [TestCase(WrongChainCertificatePath, SslPolicyErrors.None, false)]
    [TestCase(DefaultCertificatePath, SslPolicyErrors.None, true)]
    public void Validate_WithGivenCertAndErrors_ShouldReturnExpected(string certPath, SslPolicyErrors errors,
        bool expected)
    {
        // arrange
        var validator = CreateValidator();
        var certToTest = GetCertificate(certPath);

        // act
        var result = validator.Validate(certToTest, X509Chain.Create(), errors);

        // assert
        Assert.That(result, Is.EqualTo(expected));
    }

    private static SslValidator CreateValidator(
        string certificatePath = DefaultCertificatePath,
        string crlPath = CrlPath)
    {
        return new SslValidator(new Pkcs12CertificateOptions
        {
            CertificateFilePath = certificatePath,
            Password = Password,
            RevocationListFilePath = crlPath
        });
    }

    private static X509Certificate2 GetCertificate(string certificatePath = DefaultCertificatePath)
    {
        return certificatePath is null
            ? null
            : X509CertificateLoader.LoadPkcs12FromFile(certificatePath, Password);
    }
}
