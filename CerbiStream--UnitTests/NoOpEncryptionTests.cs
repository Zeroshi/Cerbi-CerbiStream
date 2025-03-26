using CerbiClientLogging.Implementations;
using CerbiStream.Interfaces;
using System;
using Xunit;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

public class NoOpEncryptionTests
{
    [Fact]
    public void NoOpEncryption_ShouldHaveDisabledEncryption()
    {
        var encryption = new NoOpEncryption();
        Assert.False(encryption.IsEnabled);
        Assert.Equal(EncryptionType.None, encryption.EncryptionMethod);
    }

    [Fact]
    public void Encrypt_ShouldReturnOriginalString()
    {
        var encryption = new NoOpEncryption();
        string input = "no-encryption-needed";
        string result = encryption.Encrypt(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalString()
    {
        var encryption = new NoOpEncryption();
        string input = "already-plaintext";
        string result = encryption.Decrypt(input);

        Assert.Equal(input, result);
    }
}
