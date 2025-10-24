using System;
using System.Text;
using Xunit;

namespace CerbiStream.Tests
{
 public class AesRoundtripTests
 {
 [Fact(DisplayName = "AesEncryption roundtrip - encrypt then decrypt returns original")]
 public void AesRoundtrip_EncryptThenDecrypt_ReturnsOriginal()
 {
 var key = Encoding.UTF8.GetBytes("1234567890123456");
 var iv = Encoding.UTF8.GetBytes("1234567890123456");
 var aes = new AesEncryption(key, iv, true);
 var original = "Sensitive data for testing";
 var enc = aes.Encrypt(original);
 var dec = aes.Decrypt(enc);
 Assert.Equal(original, dec);
 }

 [Fact(DisplayName = "AesEncryption invalid base64 decrypt throws")]
 public void AesDecrypt_InvalidBase64_ThrowsFormatException()
 {
 var key = Encoding.UTF8.GetBytes("1234567890123456");
 var iv = Encoding.UTF8.GetBytes("1234567890123456");
 var aes = new AesEncryption(key, iv, true);
 Assert.Throws<FormatException>(() => aes.Decrypt("not-base64"));
 }
 }
}
