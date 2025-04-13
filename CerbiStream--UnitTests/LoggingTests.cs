using CerbiClientLogging.Classes;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using Microsoft.Extensions.Logging;
using Moq;
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

        _logging = new Logging(_mockLogger.Object, _mockQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object);
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
}
