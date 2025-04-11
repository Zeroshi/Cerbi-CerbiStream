using CerbiStream.Logging;
using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using Xunit;

namespace CerbiStream.Tests
{
    public class CerbiStreamLoggerTests
    {
        [Fact]
        public void Log_ShouldNotThrow_WhenLoggerIsEnabled()
        {
            var options = new CerbiStreamOptions();
            var logger = new CerbiStreamLogger("TestCategory", options);

            var logLevel = LogLevel.Information;
            var eventId = new EventId(1, "TestEvent");
            var state = "test log";
            Exception ex = null;

            logger.Log(logLevel, eventId, state, ex, (s, e) => "Formatted log");

            Assert.True(true); // No exception means pass
        }

        [Fact]
        public void IsEnabled_ShouldReturnTrue()
        {
            var options = new CerbiStreamOptions();
            var logger = new CerbiStreamLogger("TestCategory", options);

            Assert.True(logger.IsEnabled(LogLevel.Debug));
        }
    }
}