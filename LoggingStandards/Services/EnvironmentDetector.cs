using System;
using System.Reflection;

namespace CerbiStream.Services;

/// <summary>
/// Auto-detects runtime environment metadata.
/// Values are cached on first access (lazy initialization).
/// </summary>
public static class EnvironmentDetector
{
    private static readonly Lazy<string> _environment = new(DetectEnvironment);
    private static readonly Lazy<string> _instanceId = new(DetectInstanceId);
    private static readonly Lazy<string?> _appVersion = new(DetectAppVersion);

    /// <summary>
    /// Detected environment (Development, Staging, Production, etc.)
    /// Fallback chain: ASPNETCORE_ENVIRONMENT → DOTNET_ENVIRONMENT → "Unknown"
    /// </summary>
    public static string Environment => _environment.Value;

    /// <summary>
    /// Detected instance/pod/container ID.
    /// Fallback chain: HOSTNAME → COMPUTERNAME → MachineName
    /// </summary>
    public static string InstanceId => _instanceId.Value;

    /// <summary>
    /// Detected application version from entry assembly.
    /// Fallback chain: APP_VERSION env var → Assembly version → null
    /// </summary>
    public static string? AppVersion => _appVersion.Value;

    private static string DetectEnvironment()
    {
        return System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Unknown";
    }

    private static string DetectInstanceId()
    {
        // Kubernetes/Docker typically set HOSTNAME
        return System.Environment.GetEnvironmentVariable("HOSTNAME")
            ?? System.Environment.GetEnvironmentVariable("COMPUTERNAME")
            ?? System.Environment.MachineName;
    }

    private static string? DetectAppVersion()
    {
        // Explicit env var takes precedence (set by CI/CD)
        var envVersion = System.Environment.GetEnvironmentVariable("APP_VERSION");
        if (!string.IsNullOrEmpty(envVersion))
            return envVersion;

        // Fall back to assembly version
        try
        {
            var assembly = Assembly.GetEntryAssembly();
            var version = assembly?.GetName().Version;
            if (version != null && version != new Version(0, 0, 0, 0))
            {
                return version.ToString();
            }

            // Try informational version (includes semver suffix)
            var infoVersion = assembly?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            
            if (!string.IsNullOrEmpty(infoVersion))
            {
                // Strip metadata after + (e.g., "1.2.3+abc123" → "1.2.3")
                var plusIndex = infoVersion.IndexOf('+');
                return plusIndex > 0 ? infoVersion[..plusIndex] : infoVersion;
            }
        }
        catch
        {
            // Ignore reflection errors
        }

        return null;
    }
}
