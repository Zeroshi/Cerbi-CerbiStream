using CerbiClientLogging.Classes;
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

    //  [Fact]
    //    public async Task Should_Use_Correct_Queue_When_Requested()
    //   {
    // Arrange
    //       var mockRabbitQueue = new Mock<IQueue>();
    //       var mockAzureQueue = new Mock<IQueue>();
    //       var mockKafkaQueue = new Mock<IQueue>();

    //       mockRabbitQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()))
    //                      .ReturnsAsync(true);
    //       mockAzureQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()))
    //                     .ReturnsAsync(true);
    //       mockKafkaQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()))
    //                    .ReturnsAsync(true);

    //       var loggingRabbit = new Logging(_mockLogger.Object, mockRabbitQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object);
    //       var loggingAzure = new Logging(_mockLogger.Object, mockAzureQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object);
    //       var loggingKafka = new Logging(_mockLogger.Object, mockKafkaQueue.Object, _mockJsonConverter.Object, _mockEncryption.Object);

    // Act
    //       bool rabbitResult = await loggingRabbit.LogEventAsync("RabbitMQ Log", LogLevel.Information);
    //       bool azureResult = await loggingAzure.LogEventAsync("Azure Log", LogLevel.Information);
    //       bool kafkaResult = await loggingKafka.LogEventAsync("Kafka Log", LogLevel.Information);

    // Assert
    //       Assert.True(rabbitResult, "RabbitMQ logging should succeed.");
    //       Assert.True(azureResult, "Azure logging should succeed.");
    //       Assert.True(kafkaResult, "Kafka logging should succeed.");

    // ✅ Verify each queue was used once
    //       mockRabbitQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once, "RabbitMQ should receive one message.");
    //      mockAzureQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once, "Azure Service Bus should receive one message.");
    //       mockKafkaQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once, "Kafka should receive one message.");
    //  }





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

    //   [Fact]
    //   public async Task Should_Log_Performance_Data_Correctly()
    //   {
    // Arrange
    //       string eventName = "PerformanceTest";
    //       long elapsedMilliseconds = 500;
    //       var expectedMetadataKey = "ElapsedMilliseconds";

    //       _mockQueue.Setup(q => q.SendMessageAsync(It.Is<string>(msg => msg.Contains(expectedMetadataKey)), It.IsAny<Guid>()))
    //                 .ReturnsAsync(true);

    // Act
    //       bool result = await _logging.LogPerformanceAsync(eventName, elapsedMilliseconds);

    // ✅ Ensure the function returns `true`
    //       Assert.True(result, "LogPerformanceAsync should return true when logging succeeds.");

    // ✅ Verify that the queue received a message with the correct metadata
    //       _mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(msg => msg.Contains(expectedMetadataKey)), It.IsAny<Guid>()),
    //                         Times.Once, "Log should include ElapsedMilliseconds.");
    //}



    //    [Fact]
    //    public async Task Should_Log_Performance_Data_With_Metadata_Correctly()
    //    {
    //        string eventName = "PerformanceTest";
    //       long elapsedMilliseconds = 500;
    //        var expectedMetadataKey = "ElapsedMilliseconds";

    //        _mockQueue.Setup(q => q.SendMessageAsync(It.Is<string>(msg => msg.Contains(expectedMetadataKey)), It.IsAny<Guid>()))
    //                 .ReturnsAsync(true);

    //       bool result = await _logging.LogPerformanceAsync(eventName, elapsedMilliseconds);

    //       Assert.True(result, "LogPerformanceAsync should return true when logging succeeds.");

    //      _mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(msg => msg.Contains(expectedMetadataKey)), It.IsAny<Guid>()),
    //                        Times.Once, "Log should include ElapsedMilliseconds.");
    //  }



    //   [Fact]
    //   public void Should_Correctly_Convert_To_Json()
    //    {
    //        var testObject = new { Name = "Test", Value = 123 };
    //        string result = _mockJsonConverter.Object.ConvertMessageToJson(testObject);
    //        Assert.Equal("{}", result);
    //    }
}
