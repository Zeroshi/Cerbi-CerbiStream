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
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

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
    public async Task Should_Encrypt_FullPayload_Correctly()
    {
        var mockLogger = new Mock<ILogger<Logging>>();
        var mockQueue = new Mock<ISendMessage>();
        var mockJson = new Mock<IConvertToJson>();
        var mockEncryption = new Mock<IEncryption>();
        var options = new CerbiStreamOptions().WithEncryptionMode(EncryptionType.AES);

        mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{\"LogData\":\"something\"}");
        mockEncryption.Setup(e => e.IsEnabled).Returns(true);
        mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("ENCRYPTED-PAYLOAD");

        var logger = new Logging(mockLogger.Object, mockQueue.Object, mockJson.Object, mockEncryption.Object, options);

        var metadata = new Dictionary<string, object>();

        await logger.LogEventAsync("Test Event", LogLevel.Information, metadata);

        mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(p => p.Contains("ENCRYPTED-PAYLOAD")), It.IsAny<string>()), Times.Once);
    }



    [Fact]
    public async Task Should_Handle_Empty_Message_Correctly()
    {
        bool result = await _logging.LogEventAsync("", LogLevel.Information);
        Assert.False(result);
    }
}
