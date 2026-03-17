using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Cekok.Api.Services;

/// <summary>
/// AES-256-GCM encryption for SSH passwords stored in database.
/// </summary>
public class EncryptionService(IConfiguration config)
{
    private readonly byte[] _key = DeriveKey(
        Environment.GetEnvironmentVariable("CEKOK_ENCRYPTION_SECRET")
        ?? config["Encryption:Secret"]
        ?? throw new Exception("Encryption secret not configured"));

    private static byte[] DeriveKey(string secret)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plaintext)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Format: base64(nonce) + "." + base64(cipher) + "." + base64(tag)
        return $"{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(cipherBytes)}.{Convert.ToBase64String(tag)}";
    }

    public string Decrypt(string encrypted)
    {
        var parts = encrypted.Split('.');
        if (parts.Length != 3) throw new FormatException("Invalid encrypted format");

        var nonce = Convert.FromBase64String(parts[0]);
        var cipherBytes = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, tag.Length);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
