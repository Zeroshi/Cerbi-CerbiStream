using System;
using System.Threading.Tasks;
using CerberusClientLogging.Implementations;
using CerberusClientLogging.Interfaces;
using CerberusLogging.Classes.Enums;
using Microsoft.Extensions.Logging;
using Moq;
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

        public LoggingTests()
        {
            _mockTransactionDestination = new Mock<ITransactionDestination>();
            _mockEncryption = new Mock<IEncryption>();
            _mockEnvironment = new Mock<IEnvironment>();
            _mockIdentifiableInformation = new Mock<IIdentifiableInformation>();

            _logging = new Logging();
        }

        [Fact]
        public async Task SendApplicationLogAsync_ShouldReturnTrue()
        {
            // Arrange
            string applicationMessage = "Test log message";
            string currentMethod = "UnitTestMethod";
            LogLevel logLevel = LogLevel.Information;
            string log = "Test log";
            string applicationName = "TestApp";
            string platform = "TestPlatform";
            bool onlyInnerException = false;
            string note = "TestNote";
            Exception error = null;
            string payload = "TestPayload";

            // Act
            var result = await _logging.SendApplicationLogAsync(
                applicationMessage,
                currentMethod,
                logLevel,
                log,
                applicationName,
                platform,
                onlyInnerException,
                note,
                error,
                _mockTransactionDestination.Object,
                TransactionDestinationTypes.Other,
                _mockEncryption.Object,
                _mockEnvironment.Object,
                _mockIdentifiableInformation.Object,
                payload);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SendApplicationLogAsync_ShouldHandleExceptions()
        {
            // Arrange
            string applicationMessage = "";
            string currentMethod = "UnitTestMethod";
            LogLevel logLevel = LogLevel.Information;
            string log = "Test log";
            string applicationName = "TestApp";
            string platform = "TestPlatform";
            bool onlyInnerException = false;
            string note = "TestNote";
            Exception error = new Exception("Test exception");
            string payload = "TestPayload";

            // Act & Assert
            var result = await _logging.SendApplicationLogAsync(
                applicationMessage,
                currentMethod,
                logLevel,
                log,
                applicationName,
                platform,
                onlyInnerException,
                note,
                error,
                _mockTransactionDestination.Object,
                TransactionDestinationTypes.Other,
                _mockEncryption.Object,
                _mockEnvironment.Object,
                _mockIdentifiableInformation.Object,
                payload);

            Assert.True(result); // Should not crash even if an exception is logged
        }

        [Fact]
        public async Task SendApplicationLogAsync_ShouldHandleEmptyLog()
        {
            // Arrange
            string applicationMessage = "";
            string currentMethod = "UnitTestMethod";
            LogLevel logLevel = LogLevel.Information;
            string log = "";
            string applicationName = "TestApp";
            string platform = "TestPlatform";
            bool onlyInnerException = false;
            string note = "TestNote";
            Exception error = null;
            string payload = "TestPayload";

            // Act
            var result = await _logging.SendApplicationLogAsync(
                applicationMessage,
                currentMethod,
                logLevel,
                log,
                applicationName,
                platform,
                onlyInnerException,
                note,
                error,
                _mockTransactionDestination.Object,
                TransactionDestinationTypes.Other,
                _mockEncryption.Object,
                _mockEnvironment.Object,
                _mockIdentifiableInformation.Object,
                payload);

            // Assert
            Assert.True(result);
        }
    }
}
