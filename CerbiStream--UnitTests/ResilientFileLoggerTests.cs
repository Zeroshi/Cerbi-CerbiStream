using CerbiStream.FileLogging;
using System;
using System.IO;
using Xunit;

namespace CerbiStream.Tests
{
    public class ResilientFileLoggerTests
    {
        [Fact]
        public void Log_ShouldWriteToPrimary_WhenPrimaryAccessible()
        {
            string primaryPath = Path.GetTempFileName();
            string fallbackPath = Path.GetTempFileName();
            var logger = new ResilientFileLogger(primaryPath, fallbackPath, 3, TimeSpan.FromMilliseconds(10));

            var logEntry = new { Message = "Primary log test" };
            logger.Log(logEntry);

            string content = File.ReadAllText(primaryPath);
            Assert.Contains("Primary log test", content);

            File.Delete(primaryPath);
            File.Delete(fallbackPath);
        }

        [Fact]
        public void Log_ShouldFallback_WhenPrimaryFails()
        {
            string invalidPrimaryPath = "Z:\\this\\path\\should\\fail.log"; // Make sure this fails on your system
            string fallbackPath = Path.GetTempFileName();
            var logger = new ResilientFileLogger(invalidPrimaryPath, fallbackPath, 1, TimeSpan.FromMilliseconds(10));

            var logEntry = new { Message = "Fallback log test" };
            logger.Log(logEntry);

            string content = File.ReadAllText(fallbackPath);
            Assert.Contains("Fallback log test", content);

            File.Delete(fallbackPath);
        }
    }
}
