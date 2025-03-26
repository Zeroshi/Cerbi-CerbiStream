using CerbiClientLogging.Interfaces;
using System;
using System.Text;
using Xunit;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

public class AesEncryptionTests
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes
    private readonly byte[] _iv = Encoding.UTF8.GetBytes("1234567890123456");  // 16 bytes

    [Fact]
    public void AesEncryption_ShouldBeEnabled_WhenConstructedWithTrue()
    {
        var aes = new AesEncryption(_key, _iv, true);
        Assert.True(aes.IsEnabled);
        Assert.Equal(EncryptionType.AES, aes.EncryptionMethod);
    }

    [Fact]
    public void AesEncryption_ShouldBeDisabled_WhenConstructedWithFalse()
    {
        var aes = new AesEncryption(_key, _iv, false);
        Assert.False(aes.IsEnabled);
    }

    [Fact]
    public void EncryptDecrypt_ShouldRoundTripSuccessfully()
    {
        var aes = new AesEncryption(_key, _iv, true);
        string original = "Top Secret Data";
        string encrypted = aes.Encrypt(original);
        string decrypted = aes.Decrypt(encrypted);

        Assert.NotEqual(original, encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_ShouldReturnOriginal_WhenDisabled()
    {
        var aes = new AesEncryption(_key, _iv, false);
        string input = "plaintext";
        string result = aes.Encrypt(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginal_WhenDisabled()
    {
        var aes = new AesEncryption(_key, _iv, false);
        string input = "not-actually-encrypted";
        string result = aes.Decrypt(input);

        Assert.Equal(input, result);
    }
}
