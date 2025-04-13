using CerbiStream.FileLogging;
using NSubstitute;
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
            // Use the real FileWriter to verify real file writing.
            var logger = new ResilientFileLogger(primaryPath, fallbackPath, 3, TimeSpan.FromMilliseconds(10), new FileWriter());

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
            var mockFileWriter = Substitute.For<IFileWriter>();
            // Simulate failure on primary write by throwing an exception when the primary path is used.
            mockFileWriter
                .When(x => x.AppendText(Arg.Is("invalid_path.log"), Arg.Any<string>()))
                .Do(x => { throw new Exception("Primary failed"); });

            string invalidPrimaryPath = "invalid_path.log"; // Clearly invalid path.
            string fallbackPath = Path.GetTempFileName();
            var logger = new ResilientFileLogger(invalidPrimaryPath, fallbackPath, 1, TimeSpan.FromMilliseconds(10), mockFileWriter);

            var logEntry = new { Message = "Fallback log test" };
            logger.Log(logEntry);

            // Instead of reading from file (since the substitute won't write anything),
            // verify that AppendText was called on fallbackPath with the expected content.
            mockFileWriter.Received().AppendText(
                fallbackPath,
                Arg.Is<string>(s => s.Contains("Fallback log test"))
            );
        }
    }
}
