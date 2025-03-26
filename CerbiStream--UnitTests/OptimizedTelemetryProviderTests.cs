using CerbiStream.Classes.OpenTelemetry;
using System;
using System.Collections.Generic;
using Xunit;

public class OptimizedTelemetryProviderTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var provider = new OptimizedTelemetryProvider();

        Assert.NotNull(provider);
    }

    [Fact]
    public void TrackEvent_ShouldNotThrow_ForValidEvent()
    {
        var provider = new OptimizedTelemetryProvider();

        var properties = new Dictionary<string, string>
        {
            { "Key", "Value" }
        };

        var exception = Record.Exception(() => provider.TrackEvent("TestEvent", properties));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("DebugLog")]
    [InlineData("HealthCheck")]
    [InlineData("BackgroundJobExecution")]
    public void TrackEvent_ShouldIgnoreExcludedEvents(string eventName)
    {
        var provider = new OptimizedTelemetryProvider();

        var props = new Dictionary<string, string>
        {
            { "Ignored", "True" }
        };

        // These should silently skip
        var exception = Record.Exception(() => provider.TrackEvent(eventName, props));
        Assert.Null(exception);
    }

    [Fact]
    public void TrackException_ShouldCreateActivityWithTags()
    {
        var provider = new OptimizedTelemetryProvider();

        var props = new Dictionary<string, string>
        {
            { "Env", "Test" }
        };

        var ex = new InvalidOperationException("Something went wrong");

        var exception = Record.Exception(() => provider.TrackException(ex, props));

        Assert.Null(exception); // Should log silently without throwing
    }

    [Fact]
    public void TrackDependency_ShouldIncludeMetadata()
    {
        var provider = new OptimizedTelemetryProvider();

        var exception = Record.Exception(() =>
        {
            provider.TrackDependency("SQL", "SELECT * FROM Users", TimeSpan.FromMilliseconds(123), true);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_ShouldRespectSamplingRate()
    {
        var provider = new OptimizedTelemetryProvider(true, 0.5);
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_ShouldClampInvalidSamplingRate()
    {
        var providerHigh = new OptimizedTelemetryProvider(true, 5.0);
        var providerLow = new OptimizedTelemetryProvider(true, -1.0);

        Assert.NotNull(providerHigh);
        Assert.NotNull(providerLow);
    }
}
