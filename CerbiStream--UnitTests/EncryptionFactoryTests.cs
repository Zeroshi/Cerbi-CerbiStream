using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiStream.Encryption;
using CerbiStream.Helpers;
using CerbiStream.Logging.Configuration;
using System;
using System.Text;
using Xunit;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

public class EncryptionFactoryTests
{
    [Fact]
    public void GetEncryption_ShouldReturn_NoOpEncryption_WhenEncryptionTypeIsNone()
    {
        var options = new CerbiStreamOptions().WithEncryptionMode(EncryptionType.None);
        var encryption = EncryptionFactory.GetEncryption(options);

        Assert.IsType<NoOpEncryption>(encryption);
        Assert.False(encryption.IsEnabled);
        Assert.Equal("test", encryption.Encrypt("test"));
    }

    [Fact]
    public void GetEncryption_ShouldReturn_Base64Encryption_WhenEncryptionTypeIsBase64()
    {
        var options = new CerbiStreamOptions().WithEncryptionMode(EncryptionType.Base64);
        var encryption = EncryptionFactory.GetEncryption(options);

        Assert.IsType<EncryptionImplementation>(encryption);
        Assert.True(encryption.IsEnabled);
        Assert.Equal(EncryptionType.Base64, encryption.EncryptionMethod);

        var data = "base64 test";
        var encrypted = encryption.Encrypt(data);
        var decrypted = encryption.Decrypt(encrypted);

        Assert.NotEqual(data, encrypted);
        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void GetEncryption_ShouldReturn_AesEncryption_WhenEncryptionTypeIsAES_WithCustomKeyIV()
    {
        var key = EncryptionHelpers.GenerateRandomKey();
        var iv = EncryptionHelpers.GenerateRandomIV();
        var options = new CerbiStreamOptions()
            .WithEncryptionMode(EncryptionType.AES)
            .WithEncryptionKey(key, iv);

        var encryption = EncryptionFactory.GetEncryption(options);

        Assert.IsType<AesEncryption>(encryption);
        Assert.True(encryption.IsEnabled);
        Assert.Equal(EncryptionType.AES, encryption.EncryptionMethod);

        var data = "aes test";
        var encrypted = encryption.Encrypt(data);
        var decrypted = encryption.Decrypt(encrypted);

        Assert.NotEqual(data, encrypted);
        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void GetEncryption_ShouldThrow_WhenAesModeWithoutKeyIv()
    {
        var options = new CerbiStreamOptions().WithEncryptionMode(EncryptionType.AES);
        Assert.Throws<InvalidOperationException>(() => EncryptionFactory.GetEncryption(options));
    }

    [Fact]
    public void GetEncryption_ShouldReturn_NoOp_WhenEncryptionModeIsUnknown()
    {
        var options = new CerbiStreamOptions();
        typeof(CerbiStreamOptions).GetProperty("EncryptionMode")!.SetValue(options, (EncryptionType)(-1));

        var encryption = EncryptionFactory.GetEncryption(options);

        Assert.IsType<NoOpEncryption>(encryption);
        Assert.False(encryption.IsEnabled);
    }
}
