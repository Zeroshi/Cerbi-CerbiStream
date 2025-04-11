
using CerbiStream.Classes.FileLogging;
using CerbiStream.FileLogging;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace CerbiStream.Tests
{
    public class FileFallbackProviderTests
    {
        [Fact]
        public void CreateLogger_ReturnsLoggerAdapter()
        {
            // Arrange
            var options = new FileFallbackOptions();
            var provider = new FileFallbackProvider(options);

            // Act
            var logger = provider.CreateLogger("TestCategory");

            // Assert
            Assert.NotNull(logger);
            Assert.IsType<FileLoggerAdapter>(logger);
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Arrange
            var options = new FileFallbackOptions();
            var provider = new FileFallbackProvider(options);

            // Act & Assert
            var ex = Record.Exception(() => provider.Dispose());
            Assert.Null(ex);
        }
    }
}
