# CerbiStream SDK - TenantId from Config & Auto-Detection Update

**Repository:** `Cerbi-CerbiStream`  
**Date:** 2026-02-03  
**Status:** 🔴 Implementation Required

---

## Summary

Update CerbiStream SDK to support:
1. **TenantId from governance config** (dashboard) with optional developer override
2. **Auto-detection** of Environment, InstanceId, AppVersion
3. **New identity fields** for better dashboard segmentation

---

## TenantId Resolution Order

```
1. Developer override      → .WithTenantId("custom")
2. Governance config       → cerbi_governance.json (from dashboard)
3. Environment variable    → CERBI_TENANT_ID
4. Error if none found
```

**Developer doesn't need to know TenantId** - it comes from the governance config downloaded from dashboard.

---

## Changes Required

### 1. Update Package Reference

**File:** `LoggingStandards/CerbiStream.csproj`

```xml
<PackageReference Include="Cerbi.Contracts" Version="1.1.0" />
```

---

### 2. Create EnvironmentDetector (NEW FILE)

**File:** `LoggingStandards/Services/EnvironmentDetector.cs`

```csharp
using System;
using System.Reflection;

namespace CerbiStream.Services;

/// <summary>
/// Auto-detects runtime environment values.
/// All detection happens ONCE at startup - zero overhead per log.
/// </summary>
public static class EnvironmentDetector
{
    // Cached values - computed once
    private static readonly Lazy<string> _environment = new(DetectEnvironmentInternal);
    private static readonly Lazy<string> _instanceId = new(DetectInstanceIdInternal);
    private static readonly Lazy<string?> _appVersion = new(DetectAppVersionInternal);
    private static readonly Lazy<string?> _deploymentId = new(DetectDeploymentIdInternal);

    /// <summary>
    /// Detects deployment environment.
    /// Source: ASPNETCORE_ENVIRONMENT → DOTNET_ENVIRONMENT → "Unknown"
    /// </summary>
    public static string Environment => _environment.Value;
    
    /// <summary>
    /// Detects instance/pod/container ID.
    /// Source: HOSTNAME → WEBSITE_INSTANCE_ID → MachineName
    /// </summary>
    public static string InstanceId => _instanceId.Value;
    
    /// <summary>
    /// Detects application version.
    /// Source: APP_VERSION env → Assembly version
    /// </summary>
    public static string? AppVersion => _appVersion.Value;
    
    /// <summary>
    /// Detects deployment/release ID.
    /// Source: DEPLOYMENT_ID → BUILD_ID → null
    /// </summary>
    public static string? DeploymentId => _deploymentId.Value;

    private static string DetectEnvironmentInternal()
    {
        return System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? System.Environment.GetEnvironmentVariable("ENVIRONMENT")
            ?? "Unknown";
    }

    private static string DetectInstanceIdInternal()
    {
        // Kubernetes / Docker
        var hostname = System.Environment.GetEnvironmentVariable("HOSTNAME");
        if (!string.IsNullOrEmpty(hostname))
            return hostname;
        
        // Azure App Service
        var websiteInstance = System.Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
        if (!string.IsNullOrEmpty(websiteInstance))
            return websiteInstance;
        
        // Azure Container Apps
        var containerAppReplica = System.Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME");
        if (!string.IsNullOrEmpty(containerAppReplica))
            return containerAppReplica;
        
        // Fallback to machine name
        return System.Environment.MachineName;
    }

    private static string? DetectAppVersionInternal()
    {
        // CI/CD pipeline can set this
        var envVersion = System.Environment.GetEnvironmentVariable("APP_VERSION");
        if (!string.IsNullOrEmpty(envVersion))
            return envVersion;
        
        // Fallback to assembly version
        try
        {
            var assembly = Assembly.GetEntryAssembly();
            var version = assembly?.GetName().Version;
            if (version != null)
                return $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch { }
        
        return null;
    }

    private static string? DetectDeploymentIdInternal()
    {
        return System.Environment.GetEnvironmentVariable("DEPLOYMENT_ID")
            ?? System.Environment.GetEnvironmentVariable("BUILD_ID")
            ?? System.Environment.GetEnvironmentVariable("RELEASE_ID");
    }
}
```

---

### 3. Create GovernanceConfigReader (NEW FILE)

**File:** `LoggingStandards/Services/GovernanceConfigReader.cs`

```csharp
using Cerbi.Contracts.Governance;
using System;
using System.IO;
using System.Text.Json;

namespace CerbiStream.Services;

/// <summary>
/// Reads governance config (from dashboard) including embedded TenantId.
/// </summary>
public class GovernanceConfigReader
{
    private readonly string _configPath;
    private GovernanceConfigDto? _cachedConfig;
    private DateTime _lastRead;

    public GovernanceConfigReader(string? configPath = null)
    {
        _configPath = configPath 
            ?? Environment.GetEnvironmentVariable("CERBI_GOVERNANCE_PATH")
            ?? Path.Combine(AppContext.BaseDirectory, "cerbi_governance.json");
    }

    /// <summary>
    /// Gets the TenantId from governance config (set by dashboard).
    /// </summary>
    public string? GetTenantId()
    {
        var config = GetConfig();
        return config?.TenantId;
    }

    /// <summary>
    /// Gets the default profile name.
    /// </summary>
    public string GetDefaultProfile()
    {
        var config = GetConfig();
        return config?.DefaultProfile ?? "default";
    }

    /// <summary>
    /// Gets the full governance config.
    /// </summary>
    public GovernanceConfigDto? GetConfig()
    {
        // Cache for 60 seconds
        if (_cachedConfig != null && DateTime.UtcNow - _lastRead < TimeSpan.FromSeconds(60))
            return _cachedConfig;

        if (!File.Exists(_configPath))
            return null;

        try
        {
            var json = File.ReadAllText(_configPath);
            _cachedConfig = JsonSerializer.Deserialize<GovernanceConfigDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            _lastRead = DateTime.UtcNow;
            return _cachedConfig;
        }
        catch
        {
            return null;
        }
    }
}
```

---

### 4. Update CerbiStreamOptions

**File:** `LoggingStandards/Configuration/CerbiStreamOptions.cs`

```csharp
using CerbiStream.Services;

namespace CerbiStream.Logging.Configuration;

public partial class CerbiStreamOptions
{
    // === IDENTITY FIELDS ===
    
    /// <summary>
    /// Tenant ID. Resolution: Developer override → Governance config → CERBI_TENANT_ID
    /// </summary>
    private string? _explicitTenantId;
    
    /// <summary>
    /// Application name. Required.
    /// </summary>
    public string? ApplicationName { get; private set; }
    
    /// <summary>
    /// Service name within the app (for microservices).
    /// </summary>
    public string? ServiceName { get; private set; }
    
    /// <summary>
    /// Environment. Auto-detected if not set.
    /// </summary>
    private string? _explicitEnvironment;
    
    /// <summary>
    /// Instance ID. Auto-detected if not set.
    /// </summary>
    private string? _explicitInstanceId;
    
    /// <summary>
    /// App version. Auto-detected if not set.
    /// </summary>
    private string? _explicitAppVersion;
    
    /// <summary>
    /// Governance config reader for TenantId resolution.
    /// </summary>
    private GovernanceConfigReader? _configReader;
    
    // === FLUENT METHODS ===
    
    /// <summary>
    /// Sets TenantId explicitly (overrides governance config).
    /// Most developers don't need this - TenantId comes from governance config.
    /// </summary>
    public CerbiStreamOptions WithTenantId(string tenantId)
    {
        _explicitTenantId = tenantId;
        return this;
    }
    
    /// <summary>
    /// Sets application identity.
    /// </summary>
    public CerbiStreamOptions WithApplicationIdentity(string appName, string? environment = null)
    {
        ApplicationName = appName;
        _explicitEnvironment = environment;
        return this;
    }
    
    /// <summary>
    /// Sets service name (for microservices).
    /// </summary>
    public CerbiStreamOptions WithServiceName(string serviceName)
    {
        ServiceName = serviceName;
        return this;
    }
    
    /// <summary>
    /// Sets governance config path. TenantId is read from this file.
    /// </summary>
    public CerbiStreamOptions WithGovernanceConfig(string configPath)
    {
        _configReader = new GovernanceConfigReader(configPath);
        return this;
    }
    
    /// <summary>
    /// Override environment (normally auto-detected).
    /// </summary>
    public CerbiStreamOptions WithEnvironment(string environment)
    {
        _explicitEnvironment = environment;
        return this;
    }
    
    /// <summary>
    /// Override instance ID (normally auto-detected).
    /// </summary>
    public CerbiStreamOptions WithInstanceId(string instanceId)
    {
        _explicitInstanceId = instanceId;
        return this;
    }
    
    /// <summary>
    /// Override app version (normally auto-detected).
    /// </summary>
    public CerbiStreamOptions WithAppVersion(string appVersion)
    {
        _explicitAppVersion = appVersion;
        return this;
    }
    
    // === RESOLVED VALUES (used by transformer) ===
    
    /// <summary>
    /// Resolves TenantId: Developer → Governance Config → Env Var → Error
    /// </summary>
    public string ResolveTenantId()
    {
        // 1. Developer override
        if (!string.IsNullOrEmpty(_explicitTenantId))
            return _explicitTenantId;
        
        // 2. Governance config (from dashboard)
        var configTenant = _configReader?.GetTenantId();
        if (!string.IsNullOrEmpty(configTenant))
            return configTenant;
        
        // 3. Environment variable
        var envTenant = Environment.GetEnvironmentVariable("CERBI_TENANT_ID");
        if (!string.IsNullOrEmpty(envTenant))
            return envTenant;
        
        // 4. Error
        throw new InvalidOperationException(
            "TenantId not found. Download governance config from dashboard, " +
            "use WithTenantId(), or set CERBI_TENANT_ID environment variable.");
    }
    
    /// <summary>
    /// Resolves Environment: Developer → Auto-detect
    /// </summary>
    public string ResolveEnvironment()
    {
        return _explicitEnvironment ?? EnvironmentDetector.Environment;
    }
    
    /// <summary>
    /// Resolves InstanceId: Developer → Auto-detect
    /// </summary>
    public string ResolveInstanceId()
    {
        return _explicitInstanceId ?? EnvironmentDetector.InstanceId;
    }
    
    /// <summary>
    /// Resolves AppVersion: Developer → Auto-detect
    /// </summary>
    public string? ResolveAppVersion()
    {
        return _explicitAppVersion ?? EnvironmentDetector.AppVersion;
    }
    
    /// <summary>
    /// Resolves DeploymentId: Auto-detect only
    /// </summary>
    public string? ResolveDeploymentId()
    {
        return EnvironmentDetector.DeploymentId;
    }
    
    /// <summary>
    /// Resolves GovernanceProfile: Developer → Config default → "default"
    /// </summary>
    public string ResolveGovernanceProfile()
    {
        return GovernanceProfile 
            ?? _configReader?.GetDefaultProfile() 
            ?? "default";
    }
}
```

---

### 5. Update ScoringEventTransformer

**File:** `LoggingStandards/Services/ScoringEventTransformer.cs`

```csharp
using Cerbi.Contracts;
using Cerbi.Contracts.Scoring;
using CerbiStream.Logging.Configuration;
using System;
using System.Collections.Generic;

namespace CerbiStream.Services;

/// <summary>
/// Transforms log entries to ScoringEventDto format.
/// Score is null - computed by Scoring API.
/// TenantId resolved from: Developer override → Governance config → Env var
/// </summary>
public static class ScoringEventTransformer
{
    public static ScoringEventDto Transform(
        object logEntry,
        string logId,
        CerbiStreamOptions options,
        IDictionary<string, object>? enrichedData = null)
    {
        var data = ExtractAsDictionary(logEntry, enrichedData);
        var violations = ExtractViolations(data);

        return new ScoringEventDto
        {
            SchemaVersion = ContractVersions.ScoringEventSchemaVersion,
            
            // Identity - resolved with fallback chain
            TenantId = options.ResolveTenantId(),
            AppName = options.ApplicationName ?? "unknown",
            ServiceName = options.ServiceName,
            Environment = options.ResolveEnvironment(),
            
            // Auto-detected
            Runtime = "dotnet",
            InstanceId = options.ResolveInstanceId(),
            AppVersion = options.ResolveAppVersion(),
            DeploymentId = options.ResolveDeploymentId(),
            
            // Log metadata
            LogId = logId,
            CorrelationId = ExtractString(data, "CorrelationId") 
                ?? ExtractString(data, "RequestId"),
            TimestampUtc = DateTime.UtcNow,
            LogLevel = ExtractString(data, "LogLevel") ?? "Information",
            
            // Governance
            GovernanceProfile = options.ResolveGovernanceProfile(),
            
            // Score is NULL - Scoring API computes it
            Score = null,
            
            // Violations with severities from governance config
            Violations = violations,
            
            GovernanceFlags = new GovernanceFlagsDto
            {
                GovernanceRelaxed = ExtractBool(data, "GovernanceRelaxed")
            },
            
            RawPayload = data
        };
    }

    // ... rest of existing helper methods (ExtractViolations, ExtractAsDictionary, etc.)
}
```

---

## Developer Experience

### Simple Setup (Most Common)

```csharp
// TenantId comes from governance config - developer doesn't need to know it!
builder.Logging.AddCerbiStream(options => options
    .WithApplicationIdentity("order-service")
    .WithServiceName("payment-api")  // Optional for microservices
    .WithGovernanceConfig("cerbi_governance.json")  // Downloaded from dashboard
    .WithQueue("AzureServiceBus", connectionString, queueName)
);
```

### With TenantId Override (Testing/Development)

```csharp
builder.Logging.AddCerbiStream(options => options
    .WithTenantId("test-tenant")  // Override for testing
    .WithApplicationIdentity("order-service", "Development")
    .WithGovernanceConfig("cerbi_governance.json")
    .WithQueue("AzureServiceBus", connectionString, queueName)
);
```

### Minimal Setup (Environment Variables)

```csharp
// All identity from environment variables
// CERBI_TENANT_ID, APP_NAME, ASPNETCORE_ENVIRONMENT
builder.Logging.AddCerbiStream(options => options
    .WithApplicationIdentity(Environment.GetEnvironmentVariable("APP_NAME"))
    .WithQueue("AzureServiceBus", connectionString, queueName)
);
```

---

## Resolution Summary

| Field | Priority 1 (Override) | Priority 2 (Config) | Priority 3 (Auto) | Priority 4 |
|-------|----------------------|---------------------|-------------------|------------|
| TenantId | `.WithTenantId()` | `cerbi_governance.json` | `CERBI_TENANT_ID` | Error |
| AppName | `.WithApplicationIdentity()` | - | - | Required |
| ServiceName | `.WithServiceName()` | - | - | Optional |
| Environment | `.WithEnvironment()` | - | `ASPNETCORE_ENVIRONMENT` | "Unknown" |
| InstanceId | `.WithInstanceId()` | - | `HOSTNAME` | MachineName |
| AppVersion | `.WithAppVersion()` | - | Assembly version | null |
| DeploymentId | - | - | `DEPLOYMENT_ID` | null |
| Profile | `.WithGovernanceProfile()` | Config default | - | "default" |

---

## File Summary

| File | Action | Description |
|------|--------|-------------|
| `CerbiStream.csproj` | MODIFY | Update Cerbi.Contracts to 1.1.0 |
| `Services/EnvironmentDetector.cs` | CREATE | Auto-detection (cached) |
| `Services/GovernanceConfigReader.cs` | CREATE | Read TenantId from config |
| `Configuration/CerbiStreamOptions.cs` | MODIFY | Add resolution methods |
| `Services/ScoringEventTransformer.cs` | MODIFY | Use resolved values |

---

## Build & Test

```bash
cd Cerbi-CerbiStream
dotnet restore
dotnet build
dotnet test
```

### Test TenantId Resolution

```csharp
[Fact]
public void ResolveTenantId_FromGovernanceConfig()
{
    // Create test governance config with TenantId
    File.WriteAllText("test_governance.json", """
    {
        "tenantId": "from-dashboard",
        "profiles": { "default": {} }
    }
    """);
    
    var options = new CerbiStreamOptions()
        .WithGovernanceConfig("test_governance.json")
        .WithApplicationIdentity("test-app");
    
    Assert.Equal("from-dashboard", options.ResolveTenantId());
}

[Fact]
public void ResolveTenantId_DeveloperOverrideWins()
{
    var options = new CerbiStreamOptions()
        .WithTenantId("developer-override")
        .WithGovernanceConfig("test_governance.json")  // Has different TenantId
        .WithApplicationIdentity("test-app");
    
    Assert.Equal("developer-override", options.ResolveTenantId());
}
```
