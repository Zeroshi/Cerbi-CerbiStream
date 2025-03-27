using CerbiClientLogging.Interfaces;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

public class CerbiLoggerTests
{
    [Fact]
    public async Task LogAsync_ShouldWriteToConsole_WhenInDebugMode()
    {
        var logger = new CerbiLogger(null, encryptionEnabled: false, debugMode: true);

        var result = await logger.LogAsync("Test debug message");

        Assert.True(result);
    }

    [Fact]
    public async Task LogAsync_ShouldReturnFalse_WhenQueueIsNull_AndNotInDebugMode()
    {
        var logger = new CerbiLogger(null, encryptionEnabled: false, debugMode: false);

        var result = await logger.LogAsync("This should fail");

        Assert.False(result);
    }

    [Fact]
    public async Task LogAsync_ShouldCallQueue_WhenConfigured()
    {
        var mockQueue = new Mock<IQueue>();
        mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                 .ReturnsAsync(true);

        var logger = new CerbiLogger(mockQueue.Object, encryptionEnabled: true, debugMode: false);

        var result = await logger.LogAsync("Send to queue");

        Assert.True(result);
        mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task LogAsync_ShouldNotCallQueue_WhenInDebugMode()
    {
        var mockQueue = new Mock<IQueue>();

        var logger = new CerbiLogger(mockQueue.Object, encryptionEnabled: true, debugMode: true);

        var result = await logger.LogAsync("Should only log locally");

        Assert.True(result);
        mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }
}
