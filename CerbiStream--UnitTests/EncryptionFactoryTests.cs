using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiStream.Encryption;
using CerbiStream.Logging.Configuration;
using System;
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
    }

    [Fact]
    public void GetEncryption_ShouldReturn_Base64EncryptionImplementation_WhenEncryptionTypeIsBase64()
    {
        var options = new CerbiStreamOptions().WithEncryptionMode(EncryptionType.Base64);
        var encryption = EncryptionFactory.GetEncryption(options);

        Assert.IsType<EncryptionImplementation>(encryption);
        Assert.True(encryption.IsEnabled);
        Assert.Equal(EncryptionType.Base64, encryption.EncryptionMethod);
    }

    [Fact]
    public void GetEncryption_ShouldReturn_AesEncryptionImplementation_WhenEncryptionTypeIsAES()
    {
        var options = new CerbiStreamOptions().WithEncryptionMode(EncryptionType.AES);
        var encryption = EncryptionFactory.GetEncryption(options);

        Assert.IsType<EncryptionImplementation>(encryption);
        Assert.True(encryption.IsEnabled);
        Assert.Equal(EncryptionType.AES, encryption.EncryptionMethod);
    }

    [Fact]
    public void GetEncryption_ShouldReturn_DisabledEncryptionImplementation_WhenUnknownType()
    {
        var options = new CerbiStreamOptions();
        typeof(CerbiStreamOptions).GetProperty("EncryptionMode")!.SetValue(options, (EncryptionType)(-1));

        var encryption = EncryptionFactory.GetEncryption(options);

        Assert.IsType<EncryptionImplementation>(encryption);
        Assert.False(encryption.IsEnabled);
    }
}
