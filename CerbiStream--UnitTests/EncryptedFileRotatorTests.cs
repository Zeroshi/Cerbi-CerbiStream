
using CerbiStream.Classes.FileLogging;
using System;
using System.IO;
using Xunit;

namespace CerbiStream.Tests
{
    public class EncryptedFileRotatorTests
    {
        [Fact]
        public void CheckAndRotateIfNeeded_ShouldCreateEncryptedArchive_WhenConditionsMet()
        {
            var tempFile = Path.GetTempFileName();
            var options = new FileFallbackOptions
            {
                FallbackFilePath = tempFile,
                MaxFileSizeBytes = 1, // Force rotation
                EncryptionKey = "12345678901234567890123456789012",
                EncryptionIV = "1234567890123456"
            };

            File.WriteAllText(tempFile, "test log");

            var rotator = new EncryptedFileRotator(options);
            rotator.CheckAndRotateIfNeeded();

            var logDir = Path.GetDirectoryName(tempFile)!;
            var encFiles = Directory.GetFiles(logDir, "*.enc");

            Assert.NotEmpty(encFiles);
        }

    }
}