// CerbiStream -- Additional Targeted Tests

using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiStream.Interfaces;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CerbiStream_UnitTests
{
    public class ExtendedCoverageTests
    {
        [Fact]
        public async Task Should_Drop_Log_When_Governance_Fails()
        {
            var mockQueue = new Mock<ISendMessage>();
            var mockJson = new Mock<IConvertToJson>();
            var mockEncrypt = new Mock<IEncryption>();
            var options = new CerbiStreamOptions()
                .WithGovernanceValidator((profile, data) => false);

            var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

            var result = await logger.LogEventAsync("Test Governance", LogLevel.Warning);

            Assert.False(result);
        }

        [Fact]
        public async Task Should_Not_Send_When_Queue_Disabled()
        {
            var mockQueue = new Mock<ISendMessage>();
            var mockJson = new Mock<IConvertToJson>();
            var mockEncrypt = new Mock<IEncryption>();
            var options = new CerbiStreamOptions().WithDisableQueue(true);

            var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

            var result = await logger.LogEventAsync("Test Queue Disable", LogLevel.Information);

            mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.True(result);
        }

        [Fact]
        public void Should_Handle_Missing_TelemetryProvider_Gracefully()
        {
            var options = new CerbiStreamOptions();
            var telemetry = options.TelemetryProvider;
            Assert.Null(telemetry);
        }

        [Fact]
        public async Task Should_Encrypt_And_Send_When_AesEncryption_Enabled()
        {
            var mockQueue = new Mock<ISendMessage>();
            var mockJson = new Mock<IConvertToJson>();
            var mockEncrypt = new Mock<IEncryption>();
            var options = new CerbiStreamOptions()
                .WithEncryptionMode(IEncryptionTypeProvider.EncryptionType.AES)
                .WithEncryptionKey(new byte[32], new byte[16]);

            mockEncrypt.Setup(e => e.IsEnabled).Returns(true);
            mockEncrypt.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("ENCRYPTED-PAYLOAD");
            mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{\"test\":\"data\"}");

            var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

            await logger.LogEventAsync("EncryptTest", LogLevel.Information);

            mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(p => p.Contains("ENCRYPTED-PAYLOAD")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Should_Send_Plain_When_No_Encryption()
        {
            var mockQueue = new Mock<ISendMessage>();
            var mockJson = new Mock<IConvertToJson>();
            var mockEncrypt = new Mock<IEncryption>();
            var options = new CerbiStreamOptions()
                .WithEncryptionMode(IEncryptionTypeProvider.EncryptionType.None);

            mockEncrypt.Setup(e => e.IsEnabled).Returns(false);
            mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{\"test\":\"data\"}");

            var logger = new Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

            await logger.LogEventAsync("PlainTest", LogLevel.Information);

            mockQueue.Verify(q => q.SendMessageAsync(It.Is<string>(p => p.Contains("test")), It.IsAny<string>()), Times.Once);
        }
    }
}
