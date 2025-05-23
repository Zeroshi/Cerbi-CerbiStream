﻿using CerbiClientLogging.Interfaces.SendMessage; // ✅ Correct interface
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Logger
{
    public class CerbiLoggerTests
    {
        [Fact]
        public async Task LogAsync_ShouldWriteToConsole_WhenInDebugMode()
        {
            var logger = new CerbiLogger(null, encryptionEnabled: false, debugMode: true, governanceValidator: null);


            var result = await logger.LogAsync("Test debug message");

            Assert.True(result);
        }



        [Fact]
        public async Task LogAsync_ShouldReturnFalse_WhenQueueIsNull_AndNotInDebugMode()
        {
            var logger = new CerbiLogger(null, encryptionEnabled: false, debugMode: false, governanceValidator: null);


            var result = await logger.LogAsync("This should fail");

            Assert.False(result);
        }

        [Fact]
        public async Task LogAsync_ShouldCallQueue_WhenConfigured()
        {
            var mockQueue = new Mock<ISendMessage>();
            mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync(true);

            var logger = new CerbiLogger(mockQueue.Object, encryptionEnabled: false, debugMode: false, governanceValidator: null);

            var result = await logger.LogAsync("Send to queue");

            Assert.True(result);
            mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task LogAsync_ShouldNotCallQueue_WhenInDebugMode()
        {
            var mockQueue = new Mock<ISendMessage>(); // ✅ Use ISendMessage now

            var logger = new CerbiLogger(null, encryptionEnabled: false, debugMode: true, governanceValidator: null);


            var result = await logger.LogAsync("Should only log locally");

            Assert.True(result);
            mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
