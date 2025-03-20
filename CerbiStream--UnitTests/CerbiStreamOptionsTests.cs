using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Moq;
using CerbiStream.Logging.Configuration;
using CerbiStream.GovernanceAnalyzer;

namespace CerbiStream.Tests
{
    public class CerbiStreamOptionsTests
    {
        [Fact]
        public void SetQueue_ShouldUpdateQueueSettings()
        {
            var options = new CerbiStreamOptions();
            options.SetQueue("Kafka", "kafka-host", "kafka-logs");

            Assert.Equal("Kafka", options.QueueType);
            Assert.Equal("kafka-host", options.QueueHost);
            Assert.Equal("kafka-logs", options.QueueName);
        }

        [Fact]
        public void EnableDevMode_ShouldSetDevModeToTrue()
        {
            var options = new CerbiStreamOptions();
            options.EnableDevMode();

            Assert.True(options.DevModeEnabled);
        }

        [Fact]
        public void DisableDevMode_ShouldSetDevModeToFalse()
        {
            var options = new CerbiStreamOptions();
            options.DisableDevMode();

            Assert.False(options.DevModeEnabled);
        }

        [Fact]
        public void IncludeAdvancedMetadata_ShouldEnableAdvancedMetadata()
        {
            var options = new CerbiStreamOptions();
            options.IncludeAdvancedMetadata();

            Assert.True(options.AdvancedMetadataEnabled);
        }

        [Fact]
        public void ExcludeAdvancedMetadata_ShouldDisableAdvancedMetadata()
        {
            var options = new CerbiStreamOptions();
            options.ExcludeAdvancedMetadata();

            Assert.False(options.AdvancedMetadataEnabled);
        }


        [Fact]
        public void ValidateLog_WhenGovernanceDisabled_ShouldReturnTrue()
        {
            var options = new CerbiStreamOptions();
            var logData = new Dictionary<string, object> { { "UserId", 1001 } };

            bool result = options.ValidateLog("Default", logData);
            Assert.True(result);
        }
    }
}
