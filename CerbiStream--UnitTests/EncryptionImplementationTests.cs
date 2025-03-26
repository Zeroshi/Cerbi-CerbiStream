using CerbiClientLogging.Implementations;
using CerbiStream.Logging.Configuration;
using Xunit;
using System;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

public class EncryptionImplementationTests
{
    [Fact]
    public void Constructor_WithDefault_ShouldEnableEncryption()
    {
        var encryption = new EncryptionImplementation(); // This will default to Base64
        Assert.True(encryption.IsEnabled);
        Assert.Equal(EncryptionType.Base64, encryption.EncryptionMethod);
    }

    [Fact]
    public void Constructor_WithFalse_ShouldDisableEncryption()
    {
        var encryption = new EncryptionImplementation(false, EncryptionType.Base64);
        Assert.False(encryption.IsEnabled);
    }

    [Fact]
    public void Encrypt_ShouldReturnBase64_WhenEnabled()
    {
        var encryption = new EncryptionImplementation(true, EncryptionType.Base64);
        var input = "secret";
        var encrypted = encryption.Encrypt(input);

        Assert.NotEqual(input, encrypted);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input)), encrypted);
    }

    [Fact]
    public void Encrypt_ShouldReturnOriginal_WhenDisabled()
    {
        var encryption = new EncryptionImplementation(false, EncryptionType.Base64);
        var input = "secret";
        var encrypted = encryption.Encrypt(input);

        Assert.Equal(input, encrypted);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginal_WhenEnabled()
    {
        var encryption = new EncryptionImplementation(true, EncryptionType.Base64);
        var input = "secret";
        var encrypted = encryption.Encrypt(input);
        var decrypted = encryption.Decrypt(encrypted);

        Assert.Equal(input, decrypted);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginal_WhenDisabled()
    {
        var encryption = new EncryptionImplementation(false, EncryptionType.Base64);
        var input = "secret";
        var decrypted = encryption.Decrypt(input);

        Assert.Equal(input, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_ShouldRoundTrip_WithAES()
    {
        var encryption = new EncryptionImplementation(true, EncryptionType.AES);
        var input = "super secret stuff";

        var encrypted = encryption.Encrypt(input);
        var decrypted = encryption.Decrypt(encrypted);

        Assert.NotEqual(input, encrypted);
        Assert.Equal(input, decrypted);
    }
}
