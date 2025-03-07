using CerbiClientLogging.Classes;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerberusLogging.Classes.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CerbiStream_UnitTests
{
    public class LoggingTests
    {
        private readonly Logging _logging;
        private readonly Mock<ITransactionDestination> _mockTransactionDestination;
        private readonly Mock<IEncryption> _mockEncryption;
        private readonly Mock<IEnvironment> _mockEnvironment;
        private readonly Mock<IIdentifiableInformation> _mockIdentifiableInformation;
        private readonly Mock<ILogger<Logging>> _mockLogger;
        private readonly Mock<ConvertToJson> _mockJsonConverter;

        public LoggingTests()
        {
            _mockTransactionDestination = new Mock<ITransactionDestination>();
            _mockEncryption = new Mock<IEncryption>();
            _mockEnvironment = new Mock<IEnvironment>();
            _mockIdentifiableInformation = new Mock<IIdentifiableInformation>();
            _mockLogger = new Mock<ILogger<Logging>>();
            _mockJsonConverter = new Mock<ConvertToJson>();

            // Ensure Encryption Mock has Encrypt Method
            _mockEncryption.Setup(e => e.IsEnabled).Returns(true);
            _mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns((string input) => $"Encrypted({input})");

            _logging = new Logging(
                _mockLogger.Object,
                _mockTransactionDestination.Object,
                _mockJsonConverter.Object,
                _mockEncryption.Object  // ✅ Now properly passing encryption
            );
        }

        [Fact]
        public async Task SendApplicationLogAsync_ShouldReturnTrue_WithValidInput()
        {
            var result = await _logging.SendApplicationLogAsync(
                "Test log message",
                "UnitTestMethod",
                LogLevel.Information,
                "Test log",
                "TestApp",
                "TestPlatform",
                false,
                "TestNote",
                null,
                _mockTransactionDestination.Object,
                TransactionDestinationTypes.Other,
                _mockEncryption.Object,
                _mockEnvironment.Object,
                _mockIdentifiableInformation.Object,
                "TestPayload"
            );

            Assert.True(result);
        }

        [Fact]
        public async Task SendApplicationLogAsync_ShouldHandleNullValuesGracefully()
        {
            var result = await _logging.SendApplicationLogAsync(
                "Default Message",
                "DefaultMethod",
                LogLevel.Information,
                "Default Log",
                "DefaultApp",
                "DefaultPlatform",
                false,
                "Default Note",
                null,
                _mockTransactionDestination.Object,
                TransactionDestinationTypes.Other,
                _mockEncryption.Object,
                _mockEnvironment.Object,
                _mockIdentifiableInformation.Object,
                "DefaultPayload"
            );

            Assert.True(result);
        }

        [Fact]
        public async Task SendApplicationLogAsync_ShouldHandleExceptionsGracefully()
        {
            _mockTransactionDestination.Setup(t => t.SendLogAsync(It.IsAny<string>(), It.IsAny<TransactionDestinationTypes>()))
                .ThrowsAsync(new Exception("Simulated Exception"));

            var result = await _logging.SendApplicationLogAsync(
                "Error log",
                "ErrorMethod",
                LogLevel.Error,
                "Simulated Exception Occurred",
                "ErrorApp",
                "ErrorPlatform",
                false,
                "Error Note",
                new Exception("Test Exception"),
                _mockTransactionDestination.Object,
                TransactionDestinationTypes.Other,
                _mockEncryption.Object,
                _mockEnvironment.Object,
                _mockIdentifiableInformation.Object,
                "ErrorPayload"
            );

            Assert.False(result);
        }
    }
}
