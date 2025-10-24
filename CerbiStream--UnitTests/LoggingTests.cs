using CerbiClientLogging.Classes;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiStream.Interfaces;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

public class LoggingTests
{
 private readonly Mock<ISendMessage> _mockQueue;
 private readonly Mock<IConvertToJson> _mockJsonConverter;
 private readonly Mock<IEncryption> _mockEncryption;
 private readonly Logging _logging;

 public LoggingTests()
 {
 _mockQueue = new Mock<ISendMessage>();
 _mockJsonConverter = new Mock<IConvertToJson>();
 _mockEncryption = new Mock<IEncryption>();

 _mockJsonConverter.Setup(j => j.ConvertMessageToJson(It.IsAny<object>()))
 .Returns((object obj) => Newtonsoft.Json.JsonConvert.SerializeObject(obj));

 _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
 .ReturnsAsync(true);

 _mockEncryption.Setup(e => e.IsEnabled).Returns(true);
 _mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted-data");

 var options = new CerbiStreamOptions();

 _logging = new Logging(_mockQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object, options);
 }


 [Fact(DisplayName = "LogEventAsync - returns false when queue throws")]
 public async Task LogEventAsync_ReturnsFalse_When_QueueFails()
 {
 var mockLogger = new Mock<ILogger<Logging>>();
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();
 var options = new CerbiStreamOptions();

 mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
 .ThrowsAsync(new Exception("Failure"));

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

 var logger = new Logging( // ✅ ILogger<Logging>
 mockQueue.Object, // ✅ ISendMessage
 mockJson.Object, // ✅ IConvertToJson
 mockEncrypt.Object, // ✅ IEncryption
 options // ✅ CerbiStreamOptions
 );

 var result = await logger.LogEventAsync("test", LogLevel.Information);
 Assert.False(result);
 }


 [Fact(DisplayName = "LogEventAsync - valid message sends once")]
 public async Task LogEventAsync_ValidMessage_ShouldReturnTrue()
 {
 bool result = await _logging.LogEventAsync("Test message", LogLevel.Information);
 Assert.True(result);

 _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
 }

 [Fact(DisplayName = "LogEventAsync - returns false when queue reports failure")]
 public async Task LogEventAsync_WhenQueueFails_ShouldReturnFalse()
 {
 _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
 .ReturnsAsync(false);

 bool result = await _logging.LogEventAsync("Test message", LogLevel.Information);

 Assert.False(result);
 _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
 }

 [Fact(DisplayName = "EncryptInternalSecrets - encrypts APIKey when enabled")]
 public async Task EncryptMetadata_Encrypts_Sensitive_Fields()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();
 var options = new CerbiStreamOptions();

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(true);
 mockEncrypt.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted");

 var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var metadata = new Dictionary<string, object>
 {
 { "APIKey", "secret" }
 };

 var method = typeof(Logging).GetMethod("EncryptInternalSecrets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
 method.Invoke(logger, new object[] { metadata });

 Assert.Equal("encrypted", metadata["APIKey"]);
 }


 [Fact(DisplayName = "LogEventAsync - returns false on exception during send")]
 public async Task LogEventAsync_WhenExceptionOccurs_ShouldReturnFalse()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();
 var options = new CerbiStreamOptions();

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

 mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
 .ThrowsAsync(new Exception("Queue failure"));

 var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 bool result = await logger.LogEventAsync("Test message", LogLevel.Information);

 Assert.False(result);
 }


 [Fact(DisplayName = "Retry policy - applied when enabled")]
 public async Task Retry_Policy_Is_Applied_When_Enabled()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();

 var options = new CerbiStreamOptions()
 .WithQueueRetries(true, retryCount:2,50);

 mockQueue.SetupSequence(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
 .ThrowsAsync(new Exception("Transient failure"))
 .ReturnsAsync(true); // Succeeds on second attempt

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

 var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var result = await logger.LogEventAsync("retry test", LogLevel.Information);

 Assert.True(result); // ✅ Success after one retry
 mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
 }


 [Fact(DisplayName = "SendApplicationLogAsync - includes expected metadata")]
 public async Task SendApplicationLogAsync_ShouldIncludeCorrectMetadata()
 {
 string applicationMessage = "Test log";
 string currentMethod = "UnitTestMethod";
 string expectedMetadataKey = "CloudProvider";

 bool result = await _logging.SendApplicationLogAsync(
 applicationMessage, currentMethod, LogLevel.Information,
 log: "Test log entry", applicationName: "UnitTestApp",
 platform: "Windows", onlyInnerException: false, note: "Test note",
 error: null, transactionDestination: null,
 transactionDestinationTypes: null, encryption: null,
 environment: null, identifiableInformation: null, payload: null,
 cloudProvider: "Azure", instanceId: "TestInstance",
 applicationVersion: "1.0.0", region: "US-East", requestId: Guid.NewGuid().ToString());

 Assert.True(result);

 _mockQueue.Verify(q => q.SendMessageAsync(
 It.Is<string>(msg => msg.Contains($"\"{expectedMetadataKey}\":\"Azure\"")),
 It.IsAny<string>()), Times.Once);
 }

 [Fact(DisplayName = "Logging - sends to queue on LogEventAsync")]
 public async Task Logging_ShouldSendToCorrectQueue()
 {
 await _logging.LogEventAsync("Test message", LogLevel.Information);
 _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
 }

 [Fact(DisplayName = "LogPerformanceAsync - returns true and sends")]
 public async Task LogPerformanceAsync_ShouldReturnTrue()
 {
 string eventName = "PerformanceTest";
 long elapsedMilliseconds =1234;

 bool result = await _logging.LogPerformanceAsync(eventName, elapsedMilliseconds);

 Assert.True(result);
 _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
 }

 [Fact(DisplayName = "EncryptMetadata - encrypts APIKey when enabled (reflection)")]
 public void EncryptMetadata_WhenEnabled_ShouldEncryptSensitiveFields()
 {
 // Arrange
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();
 var options = new CerbiStreamOptions();

 mockEncrypt.Setup(e => e.IsEnabled).Returns(true);
 mockEncrypt.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted-data");

 var logging = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var metadata = new Dictionary<string, object>
 {
 { "APIKey", "SensitiveData" }
 };

 var method = typeof(Logging).GetMethod("EncryptInternalSecrets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
 method!.Invoke(logging, new object[] { metadata });

 Assert.Equal("encrypted-data", metadata["APIKey"]);
 }


 [Fact(DisplayName = "DisableQueue - logger skips queue send when disabled")]
 public async Task Should_Not_Send_To_Queue_When_DisableQueueSending_Is_True()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();

 var options = new CerbiStreamOptions()
 .WithDisableQueue(true); // ✅ Disable queue sending

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

 var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var result = await logger.LogEventAsync("test message", LogLevel.Information);

 Assert.True(result); // Logger still succeeds
 mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
 }


 [Fact(DisplayName = "AES Encryption - payload is base64 when AES used")]
 public async Task Logging_Should_EncryptPayload_When_UsingAesEncryption()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();

 // Provide a dummy key and IV manually
 var key = new byte[32]; // AES-256
 var iv = new byte[16]; // AES block size
 var aesEncryption = new AesEncryption(key, iv);

 var options = new CerbiStreamOptions()
 .WithEncryptionMode(EncryptionType.AES)
 .WithEncryptionKey(key, iv)
 .WithTelemetryEnrichment(false)
 .WithMetadataInjection(false);

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");

 var logger = new Logging(mockQueue.Object, mockJson.Object, aesEncryption, options);

 await logger.LogEventAsync("test event", LogLevel.Information);

 // 🔥 Correct verification
 mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(payload => payload != "{}" && IsBase64(payload)), It.IsAny<string>()), Times.Once);
 }

 // Helper function
 private static bool IsBase64(string s)
 {
 Span<byte> buffer = new Span<byte>(new byte[s.Length]);
 return Convert.TryFromBase64String(s, buffer, out _);
 }

 [Fact(DisplayName = "Base64 Encryption - payload changed when Base64 mode")]
 public async Task Logging_Should_EncryptPayload_When_UsingBase64Encryption()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var base64Encryption = new EncryptionImplementation();

 var options = new CerbiStreamOptions()
 .WithEncryptionMode(EncryptionType.Base64)
 .WithTelemetryEnrichment(false)
 .WithMetadataInjection(false);

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");

 var logger = new Logging(mockQueue.Object, mockJson.Object, base64Encryption, options);

 await logger.LogEventAsync("test event", LogLevel.Information);

 mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(p => p != "{}"), It.IsAny<string>()), Times.Once);
 }


 [Fact(DisplayName = "No Encryption - payload unchanged when no encryption")]
 public async Task Logging_Should_NotEncryptPayload_When_UsingNoEncryption()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var noOpEncryption = new NoOpEncryption();
 var options = new CerbiStreamOptions()
 .WithoutEncryption()
 .WithTelemetryEnrichment(false)
 .WithMetadataInjection(false);

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");

 var logger = new Logging(mockQueue.Object, mockJson.Object, noOpEncryption, options);

 await logger.LogEventAsync("test event", LogLevel.Information);

 mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(p => p == "{}"), It.IsAny<string>()), Times.Once);
 }

 [Fact(DisplayName = "Governance - drop log when validator returns false")]
 public async Task Should_Drop_Log_When_Governance_Fails()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();
 var options = new CerbiStreamOptions()
 .WithGovernanceValidator((profile, data) => false); // Force fail

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

 var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var result = await logger.LogEventAsync("Test", LogLevel.Information);

 Assert.False(result);
 }

 [Fact(DisplayName = "DisableQueue - skip send when queue disabled via options")]
 public async Task Should_Not_Send_When_Queue_Disabled()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();
 var options = new CerbiStreamOptions().WithDisableQueue(true);

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

 var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var result = await logger.LogEventAsync("Test", LogLevel.Information);

 mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
 Assert.True(result); // returns true even if skipped
 }

 [Fact(DisplayName = "Logging - handles missing telemetry provider gracefully")]
 public void Should_Handle_Missing_TelemetryProvider_Gracefully()
 {
 var options = new CerbiStreamOptions(); // No telemetry
 var provider = options.TelemetryProvider;
 Assert.Null(provider); // Simply ensure no null exceptions happen in normal code
 }

}
