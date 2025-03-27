using CerbiClientLogging.Classes;
using CerbiClientLogging.Implementations;
using Xunit;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;


public class convertJasonTest
{
    [Fact]
    public void ConvertToJson_WithEncryption_ShouldEncryptThenDecryptSuccessfully()
    {
        var encryption = new EncryptionImplementation(true, EncryptionType.Base64);
        var converter = new ConvertToJson();

        var obj = new { Name = "Cerbi" };
        var json = converter.ConvertMessageToJson(obj);
        var encrypted = encryption.Encrypt(json);
        var decrypted = encryption.Decrypt(encrypted);

        Assert.Equal(json, decrypted);
    }
}