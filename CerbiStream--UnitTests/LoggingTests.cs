using CerbiClientLogging.Classes;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class LoggingTests
{
    private readonly Mock<ILogger<Logging>> _mockLogger;
    private readonly Mock<IQueue> _mockQueue;
    private readonly Mock<ConvertToJson> _mockJsonConverter;
    private readonly Mock<IEncryption> _mockEncryption;
    private readonly Logging _logging;

    public LoggingTests()
    {
        _mockLogger = new Mock<ILogger<Logging>>();
        _mockQueue = new Mock<IQueue>();
        _mockJsonConverter = new Mock<ConvertToJson>();
        _mockEncryption = new Mock<IEncryption>();

        // Set up serialization
        _mockJsonConverter.Setup(j => j.ConvertMessageToJson(It.IsAny<object>()))
                  .Returns((object obj) => Newtonsoft.Json.JsonConvert.SerializeObject(obj));


        // Set up queue to return true (successful log sending)
        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                  .ReturnsAsync(true);

        // Set up encryption
        _mockEncryption.Setup(e => e.IsEnabled).Returns(true);
        _mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted-data");

        _logging = new Logging(_mockLogger.Object, _mockQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object);
    }

    /// ✅ Ensure logs are sent successfully
    [Fact]
    public async Task LogEventAsync_ValidMessage_ShouldReturnTrue()
    {
        // Act
        bool result = await _logging.LogEventAsync("Test message", LogLevel.Information);

        // Assert
        Assert.True(result);
        _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
    }

    /// ❌ Ensure log fails if queue fails
    [Fact]
    public async Task LogEventAsync_WhenQueueFails_ShouldReturnFalse()
    {
        // Arrange: Queue will fail
        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        bool result = await _logging.LogEventAsync("Test message", LogLevel.Information);

        // Assert
        Assert.False(result);
        _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
    }

    /// ❗ Ensure exceptions are logged when they occur
    [Fact]
    public async Task LogEventAsync_WhenExceptionOccurs_ShouldReturnFalse()
    {
        // Arrange: Force an exception in the queue
        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                  .ThrowsAsync(new Exception("Queue failure"));

        // Act
        bool result = await _logging.LogEventAsync("Test message", LogLevel.Information);

        // Assert
        Assert.False(result);

        // Verify that an error log was recorded
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

    /// ✅ Ensure logging correctly formats and includes metadata
    [Fact]
    public async Task SendApplicationLogAsync_ShouldIncludeCorrectMetadata()
    {
        // Arrange
        string applicationMessage = "Test log";
        string currentMethod = "UnitTestMethod";
        string expectedMetadataKey = "CloudProvider";

        // Act
        bool result = await _logging.SendApplicationLogAsync(
            applicationMessage, currentMethod, LogLevel.Information,
            log: "Test log entry", applicationName: "UnitTestApp",
            platform: "Windows", onlyInnerException: false, note: "Test note",
            error: null, transactionDestination: null,
            transactionDestinationTypes: null, encryption: null,
            environment: null, identifiableInformation: null, payload: null,
            cloudProvider: "Azure", instanceId: "TestInstance",
            applicationVersion: "1.0.0", region: "US-East", requestId: Guid.NewGuid().ToString());

        // Assert
        Assert.True(result);

        // Verify queue was called with a valid log message containing the metadata
        _mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(msg => msg.Contains($"\"{expectedMetadataKey}\":\"Azure\"")), It.IsAny<Guid>()), Times.Once);
    }

    /// ✅ Ensure the correct queue is used
    [Fact]
    public async Task Logging_ShouldSendToCorrectQueue()
    {
        // Act
        await _logging.LogEventAsync("Test message", LogLevel.Information);

        // Assert
        _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
    }

    /// ✅ Ensure performance logs are sent
    [Fact]
    public async Task LogPerformanceAsync_ShouldReturnTrue()
    {
        // Arrange
        string eventName = "PerformanceTest";
        long elapsedMilliseconds = 1234;

        // Act
        bool result = await _logging.LogPerformanceAsync(eventName, elapsedMilliseconds);

        // Assert
        Assert.True(result);
        _mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
    }

    /// ✅ Ensure metadata is encrypted when required
    [Fact]
    public void EncryptMetadata_WhenEnabled_ShouldEncryptSensitiveFields()
    {
        // Arrange
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "APIKey", "SensitiveData" },
            { "SensitiveField", "SomeSecretValue" }
        };

        // Act
        var privateMethod = typeof(Logging).GetMethod("EncryptMetadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        privateMethod.Invoke(_logging, new object[] { metadata });

        // Assert
        Assert.Equal("encrypted-data", metadata["APIKey"]);
        Assert.Equal("encrypted-data", metadata["SensitiveField"]);
    }
}
