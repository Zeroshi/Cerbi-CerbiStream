using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage; // ✅ Updated
using CerbiStream.Interfaces;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class ExtendedLoggingTests
{
    private readonly Mock<ILogger<Logging>> _mockLogger;
    private readonly Mock<ISendMessage> _mockQueue; // ✅ Use ISendMessage
    private readonly Mock<IConvertToJson> _mockJsonConverter;
    private readonly Mock<IEncryption> _mockEncryption;
    private readonly Logging _logging;

    public ExtendedLoggingTests()
    {
        _mockLogger = new Mock<ILogger<Logging>>();
        _mockQueue = new Mock<ISendMessage>();
        _mockJsonConverter = new Mock<IConvertToJson>();
        _mockEncryption = new Mock<IEncryption>();

        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(true);

        _mockJsonConverter.Setup(j => j.ConvertMessageToJson(It.IsAny<object>()))
                          .Returns("{}");

        _mockEncryption.Setup(e => e.IsEnabled).Returns(true);
        _mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted-data");

        var options = new CerbiStreamOptions()
    .WithQueueRetries(true, 3, 200); // or default

        _logging = new Logging(_mockLogger.Object, _mockQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object, options);


        _logging = new Logging(_mockLogger.Object, _mockQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object, options);
    }

    [Fact]
    public async Task Should_Log_Error_When_Queue_Fails()
    {
        _mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ThrowsAsync(new Exception("Queue failure"));

        bool result = await _logging.LogEventAsync("Test error message", LogLevel.Error);

        Assert.False(result);

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
