using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using CerbiStream.Logging.Configuration;
using CerbiStream.Interfaces;

namespace CerbiStream.Tests
{
    public class CerbiStreamOptionsTests
    {
        [Fact]
        public void WithQueue_ShouldSetValues()
        {
            var options = new CerbiStreamOptions().WithQueue("Kafka", "localhost", "my-topic");
            Assert.Equal("Kafka", options.QueueType);
            Assert.Equal("localhost", options.QueueHost);
            Assert.Equal("my-topic", options.QueueName);
        }

        [Fact]
        public void WithTelemetryProvider_ShouldAssignProvider()
        {
            var mockProvider = new Mock<ITelemetryProvider>();
            var options = new CerbiStreamOptions().WithTelemetryProvider(mockProvider.Object);
            Assert.Equal(mockProvider.Object, options.TelemetryProvider);
        }

        [Fact]
        public void WithAdvancedMetadata_ShouldEnableFlag()
        {
            var options = new CerbiStreamOptions().WithAdvancedMetadata();
            Assert.True(options.AdvancedMetadataEnabled);
        }

        [Fact]
        public void WithSecurityMetadata_ShouldEnableFlag()
        {
            var options = new CerbiStreamOptions().WithSecurityMetadata();
            Assert.True(options.SecurityMetadataEnabled);
        }

        [Fact]
        public void EnableTelemetryLogging_ShouldEnableFlag()
        {
            var options = new CerbiStreamOptions().WithTelemetryLogging();
            Assert.True(options.AlsoSendToTelemetry);
        }

        [Fact]
        public void EnableBenchmarkMode_ShouldSetAllFlagsCorrectly()
        {
            var options = new CerbiStreamOptions().EnableBenchmarkMode();
            Assert.True(options.IsBenchmarkMode);
        }

        [Fact]
        public void EnableDevModeMinimal_ShouldSetMinimalMode()
        {
            var options = new CerbiStreamOptions().EnableDevModeMinimal();
            Assert.True(options.IsMinimalMode);
        }

        [Fact]
        public void EnableDeveloperModeWithTelemetry_ShouldSetCorrectFlags()
        {
            var options = new CerbiStreamOptions().EnableDeveloperModeWithTelemetry();
            Assert.True(options.IsDevWithTelemetry);
        }

        [Fact]
        public void EnableDeveloperModeWithoutTelemetry_ShouldSetCorrectFlags()
        {
            var options = new CerbiStreamOptions().EnableDeveloperModeWithoutTelemetry();
            Assert.True(options.IsDevWithoutTelemetry);
        }

        [Fact]
        public void ShouldSkipQueueSend_WhenQueueDisabled_ShouldReturnTrue()
        {
            var options = new CerbiStreamOptions().WithDisableQueue();
            Assert.True(options.ShouldSkipQueueSend());
        }

        [Fact]
        public void ValidateLog_WhenGovernanceChecksDisabled_ShouldReturnTrue()
        {
            var options = new CerbiStreamOptions().WithGovernanceChecks(false);
            var logData = new Dictionary<string, object> { { "Key", "Value" } };
            Assert.True(options.ValidateLog("Profile", logData));
        }
    }
}
