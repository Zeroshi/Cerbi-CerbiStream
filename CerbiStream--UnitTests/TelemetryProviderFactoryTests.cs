using CerbiStream.Classes.OpenTelemetry;
using CerbiStream.Interfaces;
using System;
using Xunit;

namespace CerbiStream.Tests
{
    public class TelemetryProviderFactoryTests
    {
        [Theory]
        [InlineData("appinsights", typeof(AppInsightsTelemetryProvider))]
        [InlineData("datadog", typeof(DatadogTelemetryProvider))]
        [InlineData("opentelemetry", typeof(OpenTelemetryProvider))]
        public void CreateTelemetryProvider_ShouldReturnCorrectType_Safe(string providerName, Type expectedType)
        {
            var provider = TelemetryProviderFactory.CreateTelemetryProvider(providerName);

            Assert.NotNull(provider);
            Assert.IsType(expectedType, provider);
            Assert.IsAssignableFrom<ITelemetryProvider>(provider);
        }

        [Fact(Skip = "Requires AWS credentials. Consider mocking AWSCloudWatchTelemetryProvider.")]
        public void CreateTelemetryProvider_ShouldReturnAWSProvider()
        {
            var provider = TelemetryProviderFactory.CreateTelemetryProvider("awscloudwatch");
            Assert.NotNull(provider);
            Assert.IsType<AWSCloudWatchTelemetryProvider>(provider);
        }

        [Fact(Skip = "Requires Google ADC config. Consider mocking GCPStackdriverTelemetryProvider.")]
        public void CreateTelemetryProvider_ShouldReturnGCPProvider()
        {
            var provider = TelemetryProviderFactory.CreateTelemetryProvider("gcpstackdriver");
            Assert.NotNull(provider);
            Assert.IsType<GCPStackdriverTelemetryProvider>(provider);
        }

        [Fact]
        public void CreateTelemetryProvider_ShouldThrow_OnInvalidProvider()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                TelemetryProviderFactory.CreateTelemetryProvider("invalid"));

            Assert.Contains("Unknown telemetry provider", ex.Message);
        }
    }
}
