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

public class LoggingTests
{
    private readonly Mock<ILogger<Logging>> _mockLogger;
    private readonly Mock<ISendMessage> _mockQueue; // ✅ Updated
    private readonly Mock<ConvertToJson> _mockJsonConverter;
    private readonly Mock<IEncryption> _mockEncryption;
    private readonly Logging _logging;

    public LoggingTests()
    {
        _mockLogger = new Mock<ILogger<Logging>>();
        _mockQueue = new Mock<ISendMessage>(); // ✅ Use ISendMessage
        _mockJsonConverter = new Mock<ConvertToJson>();
        _mockEncryption = new Mock<IEncryption>();

        _mockJsonConverter.Setup(j => j.ConvertMessageToJson(It.IsAny<object>()))
            .Returns((object obj) => Newtonsoft.Json.JsonConvert.SerializeObject(obj));

        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockEncryption.Setup(e => e.IsEnabled).Returns(true);
        _mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted-data");

        var options = new CerbiStreamOptions();

        _logging = new Logging(_mockLogger.Object, _mockQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object, options);
    }

    [Fact]
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

        var logger = new Logging(mockLogger.Object, mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

        var result = await logger.LogEventAsync("test", LogLevel.Information);
        Assert.False(result);
    }


    [Fact]
    public async Task LogEventAsync_ValidMessage_ShouldReturnTrue()
    {
        bool result = await _logging.LogEventAsync("Test message", LogLevel.Information);
        Assert.True(result);

        _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LogEventAsync_WhenQueueFails_ShouldReturnFalse()
    {
        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        bool result = await _logging.LogEventAsync("Test message", LogLevel.Information);

        Assert.False(result);
        _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EncryptMetadata_Encrypts_Sensitive_Fields()
    {
        var mockLogger = new Mock<ILogger<Logging>>();
        var mockQueue = new Mock<ISendMessage>();
        var mockJson = new Mock<IConvertToJson>();
        var mockEncrypt = new Mock<IEncryption>();
        var options = new CerbiStreamOptions();

        mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
        mockEncrypt.Setup(e => e.IsEnabled).Returns(true);
        mockEncrypt.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted");

        var logger = new Logging(mockLogger.Object, mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

        var metadata = new Dictionary<string, object>
    {
        { "APIKey", "secret" },
        { "SensitiveField", "value" }
    };

        var method = typeof(Logging).GetMethod("EncryptMetadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(logger, new object[] { metadata });

        Assert.Equal("encrypted", metadata["APIKey"]);
        Assert.Equal("encrypted", metadata["SensitiveField"]);
    }


    [Fact]
    public async Task LogEventAsync_WhenExceptionOccurs_ShouldReturnFalse()
    {
        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Queue failure"));

        bool result = await _logging.LogEventAsync("Test message", LogLevel.Information);

        Assert.False(result);

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Logging failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Retry_Policy_Is_Applied_When_Enabled()
    {
        var mockLogger = new Mock<ILogger<Logging>>();
        var mockQueue = new Mock<ISendMessage>();
        var mockJson = new Mock<IConvertToJson>();
        var mockEncrypt = new Mock<IEncryption>();
        var options = new CerbiStreamOptions().WithQueueRetries(true, 2, 50);

        mockQueue.SetupSequence(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Transient failure"))
            .ReturnsAsync(true); // Should succeed on retry

        mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
        mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

        var logger = new Logging(mockLogger.Object, mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

        var result = await logger.LogEventAsync("retry test", LogLevel.Information);

        Assert.True(result); // ✅ Should pass
        mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }



    [Fact]
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

    [Fact]
    public async Task Logging_ShouldSendToCorrectQueue()
    {
        await _logging.LogEventAsync("Test message", LogLevel.Information);
        _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LogPerformanceAsync_ShouldReturnTrue()
    {
        string eventName = "PerformanceTest";
        long elapsedMilliseconds = 1234;

        bool result = await _logging.LogPerformanceAsync(eventName, elapsedMilliseconds);

        Assert.True(result);
        _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void EncryptMetadata_WhenEnabled_ShouldEncryptSensitiveFields()
    {
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "APIKey", "SensitiveData" },
            { "SensitiveField", "SomeSecretValue" }
        };

        var method = typeof(Logging).GetMethod("EncryptMetadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(_logging, new object[] { metadata });

        Assert.Equal("encrypted-data", metadata["APIKey"]);
        Assert.Equal("encrypted-data", metadata["SensitiveField"]);
    }

    [Fact]
    public async Task Should_Not_Send_To_Queue_When_DisableQueueSending_Is_True()
    {
        var mockLogger = new Mock<ILogger<Logging>>();
        var mockQueue = new Mock<ISendMessage>();
        var mockJson = new Mock<IConvertToJson>();
        var mockEncrypt = new Mock<IEncryption>();

        var options = new CerbiStreamOptions()
            .WithDisableQueue(true); // ✅ Disable queue sending

        mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
        mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

        var logger = new Logging(mockLogger.Object, mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

        var result = await logger.LogEventAsync("test message", LogLevel.Information);

        Assert.True(result); // Logger still succeeds
        mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }


    [Fact]
    public async Task Should_Encrypt_Log_Json_When_Encryption_Is_Enabled()
    {
        var mockLogger = new Mock<ILogger<Logging>>();
        var mockQueue = new Mock<ISendMessage>();
        var mockJson = new Mock<IConvertToJson>();
        var mockEncrypt = new Mock<IEncryption>();

        var options = new CerbiStreamOptions()
            .WithEncryptionMode(CerbiStream.Interfaces.IEncryptionTypeProvider.EncryptionType.AES)
            .WithEncryptionKey(new byte[16], new byte[16]);
        
        mockEncrypt.Setup(e => e.IsEnabled).Returns(true);
        mockEncrypt.Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns<string>(s => $"[ENCRYPTED]{s}[/ENCRYPTED]");

        mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>()))
            .Returns("{ \"Message\": \"Sensitive payload\" }");

        var logger = new Logging(mockLogger.Object, mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

        var result = await logger.LogEventAsync("encrypt test", LogLevel.Information);

        Assert.True(result);
        mockEncrypt.Verify(e => e.Encrypt(It.Is<string>(s => s.Contains("Sensitive payload"))), Times.Once);
        mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(s => s.StartsWith("[ENCRYPTED]")), It.IsAny<string>()), Times.Once);
    }



}
