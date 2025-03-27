using CerbiStream.Classes;
using System;
using Xunit;

public class ApplicationMetadataTests
{
    [Fact]
    public void ApplicationVersion_ShouldBeExpected()
    {
        Assert.Equal("1.2.3", ApplicationMetadata.ApplicationVersion);
    }

    [Fact]
    public void ApplicationId_ShouldBeExpected()
    {
        Assert.Equal("MyApp", ApplicationMetadata.ApplicationId);
    }

    [Fact]
    public void InstanceId_ShouldMatchMachineName()
    {
        Assert.Equal(Environment.MachineName, ApplicationMetadata.InstanceId);
    }

    [Theory]
    [InlineData("AWS_EXECUTION_ENV", "AWS")]
    [InlineData("GOOGLE_CLOUD_PROJECT", "GCP")]
    [InlineData("WEBSITE_SITE_NAME", "Azure")]
    [InlineData(null, "On-Prem")]
    public void CloudProvider_ShouldDetectCorrectly(string envVar, string expected)
    {
        // Clear all related environment variables first
        Environment.SetEnvironmentVariable("AWS_EXECUTION_ENV", null);
        Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", null);
        Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", null);

        // Set the specific one under test
        if (envVar != null)
            Environment.SetEnvironmentVariable(envVar, "test");

        // Force reload
        var cloudProvider = typeof(ApplicationMetadata)
            .GetProperty("CloudProvider")
            ?.GetValue(null, null)?.ToString();

        Assert.Equal(expected, ApplicationMetadata.CloudProvider);
    }

    [Fact]
    public void Region_ShouldReturnUnknownIfNotSet()
    {
        Environment.SetEnvironmentVariable("CLOUD_REGION", null);
        Assert.Equal("unknown-region", ApplicationMetadata.Region);
    }

    [Fact]
    public void Region_ShouldReturnSetEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("CLOUD_REGION", "us-east-42");
        Assert.Equal("us-east-42", ApplicationMetadata.Region);
    }
}
