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
        public void SetQueue_ShouldUpdateQueueSettings()
        {
            var options = new CerbiStreamOptions();
            options.SetQueue("Kafka", "kafka-host", "kafka-logs");

            Assert.Equal("Kafka", options.QueueType);
            Assert.Equal("kafka-host", options.QueueHost);
            Assert.Equal("kafka-logs", options.QueueName);
        }

        [Fact]
        public void IncludeAdvancedMetadata_ShouldEnableFlag()
        {
            var options = new CerbiStreamOptions();
            options.IncludeAdvancedMetadata();

            Assert.True(options.AdvancedMetadataEnabled);
        }

        [Fact]
        public void ExcludeAdvancedMetadata_ShouldDisableFlag()
        {
            var options = new CerbiStreamOptions();
            options.IncludeAdvancedMetadata();
            options.ExcludeAdvancedMetadata();

            Assert.False(options.AdvancedMetadataEnabled);
        }

        [Fact]
        public void IncludeSecurityMetadata_ShouldEnableFlag()
        {
            var options = new CerbiStreamOptions();
            options.IncludeSecurityMetadata();

            Assert.True(options.SecurityMetadataEnabled);
        }

        [Fact]
        public void ExcludeSecurityMetadata_ShouldDisableFlag()
        {
            var options = new CerbiStreamOptions();
            options.IncludeSecurityMetadata();
            options.ExcludeSecurityMetadata();

            Assert.False(options.SecurityMetadataEnabled);
        }

        [Fact]
        public void EnableTelemetryLogging_ShouldSetFlag()
        {
            var options = new CerbiStreamOptions();
            options.EnableTelemetryLogging();

            Assert.True(options.AlsoSendToTelemetry);
        }

        [Fact]
        public void SetTelemetryProvider_ShouldStoreProvider()
        {
            var options = new CerbiStreamOptions();
            var mockProvider = new Mock<ITelemetryProvider>();

            options.SetTelemetryProvider(mockProvider.Object);

            Assert.Equal(mockProvider.Object, options.TelemetryProvider);
        }

        [Fact]
        public void DisableConsoleOutput_ShouldSetFlag()
        {
            var options = new CerbiStreamOptions();
            options.DisableConsoleOutput();

            Assert.False(options.EnableConsoleOutput);
        }

        [Fact]
        public void DisableTelemetryEnrichment_ShouldSetFlag()
        {
            var options = new CerbiStreamOptions();
            options.DisableTelemetryEnrichment();

            Assert.False(options.EnableTelemetryEnrichment);
        }

        [Fact]
        public void DisableMetadataInjection_ShouldSetFlag()
        {
            var options = new CerbiStreamOptions();
            options.DisableMetadataInjection();

            Assert.False(options.EnableMetadataInjection);
        }

        [Fact]
        public void DisableGovernanceChecks_ShouldSetFlag()
        {
            var options = new CerbiStreamOptions();
            options.DisableGovernanceChecks();

            Assert.False(options.EnableGovernanceChecks);
        }

        [Fact]
        public void EnableBenchmarkMode_ShouldDisableAllPerformanceFeatures()
        {
            var options = new CerbiStreamOptions();
            options.EnableBenchmarkMode();

            Assert.False(options.EnableConsoleOutput);
            Assert.False(options.EnableTelemetryEnrichment);
            Assert.False(options.EnableMetadataInjection);
            Assert.False(options.EnableGovernanceChecks);
        }

        [Fact]
        public void EnableDeveloperModeWithTelemetry_ShouldSetExpectedFlags()
        {
            var options = new CerbiStreamOptions();
            options.EnableDeveloperModeWithTelemetry();

            Assert.True(options.EnableConsoleOutput);
            Assert.True(options.EnableTelemetryEnrichment);
            Assert.True(options.EnableMetadataInjection);
            Assert.False(options.EnableGovernanceChecks);
            Assert.True(options.AlsoSendToTelemetry);
        }

        [Fact]
        public void EnableDeveloperModeWithoutTelemetry_ShouldSetExpectedFlags()
        {
            var options = new CerbiStreamOptions();
            options.EnableDeveloperModeWithoutTelemetry();

            Assert.True(options.EnableConsoleOutput);
            Assert.False(options.EnableTelemetryEnrichment);
            Assert.True(options.EnableMetadataInjection);
            Assert.False(options.EnableGovernanceChecks);
            Assert.False(options.AlsoSendToTelemetry);
        }

        [Fact]
        public void EnableDevModeMinimal_ShouldDisableAllOptionalFeatures()
        {
            var options = new CerbiStreamOptions();
            options.EnableDevModeMinimal();

            Assert.True(options.EnableConsoleOutput);
            Assert.False(options.EnableTelemetryEnrichment);
            Assert.False(options.EnableMetadataInjection);
            Assert.False(options.EnableGovernanceChecks);
            Assert.False(options.AlsoSendToTelemetry);
        }

        [Fact]
        public void ValidateLog_WhenGovernanceDisabled_ShouldReturnTrue()
        {
            var options = new CerbiStreamOptions();
            var logData = new Dictionary<string, object> { { "UserId", 1001 } };

            var result = options.ValidateLog("Default", logData);

            Assert.True(result);
        }
    }
}
