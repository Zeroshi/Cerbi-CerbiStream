
using CerbiStream.FileLogging;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using Xunit;

namespace CerbiStream.Tests
{
    public class FileLoggerAdapterTests
    {
        [Fact]
        public void IsEnabled_ShouldAlwaysReturnTrue()
        {
            var logger = new FileLoggerAdapter(new ResilientFileLogger("p.log", "f.log", 1, TimeSpan.FromMilliseconds(1)), "TestCategory");
            Assert.True(logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information));
        }

        [Fact]
        public void BeginScope_ShouldReturnNull()
        {
            var logger = new FileLoggerAdapter(new ResilientFileLogger("p.log", "f.log", 1, TimeSpan.FromMilliseconds(1)), "TestCategory");
            Assert.Null(logger.BeginScope("test"));
        }

        [Fact]
        public void Log_ShouldCallResilientLogger_WithCorrectShape()
        {
            var logger = new FileLoggerAdapter(new ResilientFileLogger("p.log", "f.log", 1, TimeSpan.FromMilliseconds(1)), "TestCategory");
            logger.Log(Microsoft.Extensions.Logging.LogLevel.Warning, new Microsoft.Extensions.Logging.EventId(1), "state", null, (s, e) => s.ToString());

            // File should exist
            Assert.True(File.Exists("p.log") || File.Exists("f.log"));

            if (File.Exists("p.log")) File.Delete("p.log");
            if (File.Exists("f.log")) File.Delete("f.log");
        }
    }
}
