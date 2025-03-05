using CerberusClientLogging.Classes;
using CerberusClientLogging.Implementations;
using CerberusClientLogging.Interfaces;
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

            _logging = new Logging(
                _mockLogger.Object,
                _mockTransactionDestination.Object,
                _mockJsonConverter.Object);
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

            Assert.False(result);
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
            Assert.False(result);
        }

        [Fact]
        public async Task SendApplicationLogAsync_ShouldHandleDifferentLogLevels()
        {
            var logLevels = new[] { LogLevel.Debug, LogLevel.Warning, LogLevel.Error, LogLevel.Critical };
            foreach (var level in logLevels)
            {
                var result = await _logging.SendApplicationLogAsync(
                    "Log Message",
                    "TestMethod",
                    level,
                    "Log Content",
                    "TestApp",
                    "TestPlatform",
                    false,
                    "Note",
                    null,
                    _mockTransactionDestination.Object,
                    TransactionDestinationTypes.Other,
                    _mockEncryption.Object,
                    _mockEnvironment.Object,
                    _mockIdentifiableInformation.Object,
                    "TestPayload");

                Assert.True(result);
            }
        }

        [Fact]
        public async Task SendApplicationLogAsync_ShouldHandleNullValues()
        {
            var result = await _logging.SendApplicationLogAsync(
                null,
                null,
                LogLevel.Information,
                null,
                null,
                null,
                false,
                null,
                null,
                _mockTransactionDestination.Object,
                TransactionDestinationTypes.Other,
                null,
                null,
                null,
                null);

            Assert.False(result);
        }
    }
}
