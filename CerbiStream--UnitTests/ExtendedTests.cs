using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiStream.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;


public class ExtendedLoggingTests
{
    private readonly Mock<ILogger<Logging>> _mockLogger;
    private readonly Mock<IQueue> _mockQueue;
    private readonly Mock<IConvertToJson> _mockJsonConverter;
    private readonly Mock<IEncryption> _mockEncryption;
    private readonly Logging _logging;

    public ExtendedLoggingTests()
    {
        _mockLogger = new Mock<ILogger<Logging>>();
        _mockQueue = new Mock<IQueue>();
        _mockJsonConverter = new Mock<IConvertToJson>();
        _mockEncryption = new Mock<IEncryption>();

        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                  .ReturnsAsync(true);

        _mockJsonConverter.Setup(j => j.ConvertMessageToJson(It.IsAny<object>()))
                          .Returns("{}");

        _mockEncryption.Setup(e => e.IsEnabled).Returns(true);
        _mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted-data");

        _logging = new Logging(_mockLogger.Object, _mockQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object);
    }


    [Fact]
    public async Task Should_Log_Error_When_Queue_Fails()
    {
        // Arrange
        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                  .ThrowsAsync(new Exception("Queue failure"));

        // Act
        bool result = await _logging.LogEventAsync("Test error message", LogLevel.Error);

        // Assert
        Assert.False(result); // Ensure failure

        // ✅ Fix: Use Verify with It.IsAny<T>() and proper type matching
        _mockLogger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Logging failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()
        ), Times.Once);
    }


    [Fact]
    public async Task Should_Encrypt_Metadata_Correctly()
    {
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "APIKey", "SensitiveData" },
            { "SensitiveField", "SomeSecretValue" }
        };

        var privateMethod = typeof(Logging).GetMethod("EncryptMetadata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        privateMethod.Invoke(_logging, new object[] { metadata });

        Assert.Equal("encrypted-data", metadata["APIKey"]);
        Assert.Equal("encrypted-data", metadata["SensitiveField"]);
    }

    [Fact]
    public async Task Should_Handle_Empty_Message_Correctly()
    {
        bool result = await _logging.LogEventAsync("", LogLevel.Information);
        Assert.False(result);
    }
}
