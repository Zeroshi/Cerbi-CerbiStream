// 📄 Unit Tests for AppInsightsTelemetryProvider

using CerbiStream.Classes.OpenTelemetry;
using CerbiStream.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace CerbiStream.UnitTests.Telemetry
{
    public class AppInsightsTelemetryProviderTests
    {
        private readonly AppInsightsTelemetryProvider _provider;

        public AppInsightsTelemetryProviderTests()
        {
            // Arrange
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.DisableTelemetry = true; // prevent actual sending
            _provider = new AppInsightsTelemetryProvider();
        }

        [Fact]
        public void TrackEvent_ShouldNotThrow_WhenPropertiesProvided()
        {
            var props = new Dictionary<string, string> { { "Key", "Value" } };
            var ex = Record.Exception(() => _provider.TrackEvent("TestEvent", props));
            Assert.Null(ex);
        }

        [Fact]
        public void TrackException_ShouldNotThrow_WhenExceptionAndPropertiesProvided()
        {
            var props = new Dictionary<string, string> { { "Key", "Value" } };
            var ex = Record.Exception(() => _provider.TrackException(new Exception("test"), props));
            Assert.Null(ex);
        }

        [Fact]
        public void TrackDependency_ShouldNotThrow_WhenValidArgumentsProvided()
        {
            var ex = Record.Exception(() => _provider.TrackDependency("SQL", "DBServer", TimeSpan.FromMilliseconds(150), true));
            Assert.Null(ex);
        }

        [Fact]
        public void TrackEvent_Should_NotThrow()
        {
            var provider = new AppInsightsTelemetryProvider();
            provider.TrackEvent("TestEvent", new Dictionary<string, string> { { "Key", "Value" } });
        }

        [Fact]
        public void TrackException_Should_NotThrow()
        {
            var provider = new AppInsightsTelemetryProvider();
            provider.TrackException(new Exception("TestException"), new Dictionary<string, string> { { "Key", "Value" } });
        }

        [Fact]
        public void TrackDependency_Should_NotThrow()
        {
            var provider = new AppInsightsTelemetryProvider();
            provider.TrackDependency("HTTP", "api.example.com", TimeSpan.FromMilliseconds(123), true);
        }

        [Fact]
        public void MergeWithTelemetryContext_Should_Enrich_Properties()
        {
            // Arrange
            CerbiStream.Telemetry.TelemetryContext.ServiceName = "TestService";
            CerbiStream.Telemetry.TelemetryContext.Feature = "CheckoutFlow";

            var props = new Dictionary<string, string>();

            // Act
            var enriched = typeof(AppInsightsTelemetryProvider)
                .GetMethod("MergeWithTelemetryContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { props }) as Dictionary<string, string>;

            // Assert
            Assert.NotNull(enriched);
            Assert.Contains("ServiceName", enriched);
            Assert.Contains("Feature", enriched);
            Assert.Equal("TestService", enriched["ServiceName"]);
            Assert.Equal("CheckoutFlow", enriched["Feature"]);
        }
    }
}
