using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Shared.Contract.SslOptions;

namespace User.WebApp.Protectors;

/// <summary>
/// Data protector for Cookie authentication
/// </summary>
public class CookieProtector : IDataProtector
{
    private const int AesKeySize = 256 / 8;
    private const int AesBlockSize = 128 / 8;

    private readonly byte[] _key;

    /// <summary>
    /// Creates provider instance
    /// </summary>
    public CookieProtector(Pkcs12CertificateOptions options)
    {
        _key = [.. options.Certificate.GetRSAPrivateKey().ExportRSAPrivateKey().Take(AesKeySize)];
    }

    /// <summary>
    /// Creates protector instance
    /// </summary>
    private CookieProtector(byte[] key)
    {
        _key = key;
    }

    /// <inheritdoc />
    public IDataProtector CreateProtector(string purpose)
    {
        var purposeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(purpose));
        var purposeKey = new byte[AesKeySize];
        for (var i = 0; i < purposeKey.Length; ++i)
        {
            purposeKey[i] = (byte)(_key[i] ^ purposeBytes[i]);
        }

        return new CookieProtector(purposeKey);
    }

    /// <inheritdoc />
    public byte[] Protect(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        using var aes = CreateAes();
        return Encrypt(aes, plaintext);
    }

    /// <inheritdoc />
    public byte[] Unprotect(byte[] protectedData)
    {
        ArgumentNullException.ThrowIfNull(protectedData);
        if (protectedData.Length < AesBlockSize)
        {
            throw new ArgumentException(
                $"Protected data must be not less than {AesBlockSize} bytes",
                nameof(protectedData));
        }

        var iv = protectedData[0..AesBlockSize];
        var dataToDecrypt = protectedData.AsSpan(AesBlockSize);
        using var aes = CreateAes(iv);
        return Decrypt(aes, dataToDecrypt);
    }

    private Aes CreateAes(byte[] iv = null)
    {
        var aes = Aes.Create();
        aes.Key = _key;

        if (iv is not null)
        {
            aes.IV = iv;
        }

        return aes;
    }

    private static byte[] Encrypt(Aes aes, ReadOnlySpan<byte> data)
    {
        var aesBlocksCount = data.Length / AesBlockSize + 1;
        var buffer = new byte[AesBlockSize + aesBlocksCount * AesBlockSize];

        using var memoryStream = new MemoryStream(buffer);
        memoryStream.Write(aes.IV);

        using var encryptor = aes.CreateEncryptor();
        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
        {
            cryptoStream.Write(data);
        }

        return buffer;
    }

    private static byte[] Decrypt(Aes aes, ReadOnlySpan<byte> data)
    {
        using var decryptor = aes.CreateDecryptor();
        using var memoryStream = new MemoryStream(capacity: data.Length);
        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write, leaveOpen: true))
        {
            cryptoStream.Write(data);
        }

        return memoryStream.ToArray();
    }
}
