using CerbiClientLogging.Classes;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiStream.Configuration;
using CerbiStream.Enums;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;

public class TransactionDestinationImplementationTests
{
    [Fact]
    public async Task Logging_ShouldSendToKafka_WhenKafkaDestinationSelected()
    {
        // Arrange
        var mockQueue = Substitute.For<ISendMessage>(); // ✅ Updated interface
        mockQueue.SendMessageAsync(Arg.Any<string>(), Arg.Any<string>())
                 .Returns(Task.FromResult(true));

        var logger = Substitute.For<ILogger<Logging>>();
        var jsonConverter = new ConvertToJson();
        var encryption = new NoOpEncryption();

        var config = new CerbiStreamConfig
        {
            Destination = LoggingDestination.Kafka
        };

        var options = new CerbiStreamOptions();
        var logging = new Logging(logger, mockQueue, jsonConverter, encryption, options);

        // Act
        var result = await logging.LogEventAsync("Test message", LogLevel.Information);

        // Assert
        Assert.True(result);
        await mockQueue.Received().SendMessageAsync(Arg.Any<string>(), Arg.Any<string>()); // ✅ Updated signature
    }
}
