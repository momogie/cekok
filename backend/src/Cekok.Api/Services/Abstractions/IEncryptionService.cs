namespace Cekok.Api.Services.Abstractions;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string encrypted);
}
