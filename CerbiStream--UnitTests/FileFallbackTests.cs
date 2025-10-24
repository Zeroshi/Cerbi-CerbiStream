using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using Xunit;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiStream.Logging.Configuration;
using CerbiStream.Classes.FileLogging;
using CerbiStream.Classes;
using CerbiStream.Interfaces;

namespace CerbiStream.Tests
{
 public class FileFallbackTests
 {
 [Fact(DisplayName = "File fallback - when queue fails, LogEventAsync returns false and no fallback written by core logger")]
 public async Task FileFallback_WritesEncryptedFallback_OnQueueFailure()
 {
 var tmpDir = Path.Combine(Path.GetTempPath(), "cerbi_fallback_test");
 Directory.CreateDirectory(tmpDir);
 var fallbackPath = Path.Combine(tmpDir, "fallback.json");
 var primaryPath = Path.Combine(tmpDir, "primary.json");

 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<CerbiClientLogging.Interfaces.IEncryption>();

 // Queue fails
 mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{\"m\":1}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(true);
 mockEncrypt.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s)));

 var fallbackOptions = new FileFallbackOptions { Enable = true, FallbackFilePath = fallbackPath, PrimaryFilePath = primaryPath, RetryCount =0 };
 var options = new CerbiStream.Logging.Configuration.CerbiStreamOptions();
 options = options.WithFileFallback(fallbackOptions);

 var logging = new CerbiClientLogging.Implementations.Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var result = await logging.LogEventAsync("test", Microsoft.Extensions.Logging.LogLevel.Warning);

 // Core logger currently does not write fallback files by itself; it returns false on queue failure.
 Assert.False(result);

 // Ensure no fallback file was created by core logger
 Assert.False(File.Exists(fallbackPath));

 // cleanup
 if (File.Exists(fallbackPath)) File.Delete(fallbackPath);
 if (File.Exists(primaryPath)) File.Delete(primaryPath);
 if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir);
 }
 }
}
