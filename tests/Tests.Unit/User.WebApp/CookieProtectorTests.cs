using System.Security.Cryptography;
using Shared.Contract.SslOptions;
using Tests.Common;
using User.WebApp.Protectors;

namespace Tests.Unit.User.WebApp;

/// <summary>
/// Tests for <see cref="CookieProtector"/>
/// </summary>
[TestFixture]
public class CookieProtectorTests : TestBase
{
    private readonly Pkcs12CertificateOptions options = new()
    {
        CertificateFilePath = "TestData/ssl-validator/svc_testhost.keystore.p12",
        Password = "qwerty-123"
    };

    [Test]
    public void Protect_EmptyData_Throw()
    {
        // arrange
        var protectorProvider = new CookieProtector(options);
        var protector = protectorProvider.CreateProtector("test");

        // act && assert
        Assert.Throws<ArgumentNullException>(() => protector.Protect(null));
    }

    [Test]
    public void Unprotect_EmptyData_Throw()
    {
        // arrange
        var protectorProvider = new CookieProtector(options);
        var protector = protectorProvider.CreateProtector("test");

        // act && assert
        Assert.Throws<ArgumentNullException>(() => protector.Protect(null));
    }

    [Test]
    public void Unprotect_WrongSizeData_Throw()
    {
        // arrange
        var protectorProvider = new CookieProtector(options);
        var protector = protectorProvider.CreateProtector("test");

        // act && assert
        Assert.Throws<ArgumentException>(() => protector.Unprotect([]));
    }

    [Test]
    public void Protect_Unprocted_ShouldGetSameData()
    {
        // arrange
        byte[] dataToProtect = [0x01, 0x11, 0xAA];
        var protectorProvider = new CookieProtector(options);
        var protector = protectorProvider.CreateProtector("test");

        // act
        var encrypted = protector.Protect(dataToProtect);
        var decrypted = protector.Unprotect(encrypted);

        // assert
        Assert.That(decrypted, Is.EqualTo(dataToProtect).AsCollection);
    }

    [Test]
    public void Unprocted_WithWrongKey_Throw()
    {
        // arrange
        byte[] dataToProtect = [0x01, 0x11, 0xAA];
        var protectorProvider = new CookieProtector(options);
        var protector1 = protectorProvider.CreateProtector("test1");
        var protector2 = protectorProvider.CreateProtector("test2");

        // act
        var encrypted = protector1.Protect(dataToProtect);

        // assert
        Assert.Throws<CryptographicException>(() => protector2.Unprotect(encrypted));
    }

    [Test]
    public void CreateProtector_ShouldCreateDifferentKeys()
    {
        // arrange
        byte[] dataToProtect = [0x01, 0x11, 0xAA];
        var protectorProvider = new CookieProtector(options);

        // act
        var protector1 = protectorProvider.CreateProtector("test1");
        var protector2 = protectorProvider.CreateProtector("test2");
        var encrypted1 = protector1.Protect(dataToProtect);
        var encrypted2 = protector2.Protect(dataToProtect);

        // assert
        Assert.That(encrypted1, Is.Not.EqualTo(encrypted2).AsCollection);
    }
}
