using Cerbi.Contracts.Contracts;
using CerbiStream.GovernanceRuntime.Governance;
using CerbiStream.Logging.Configuration;
using System;
using System.Collections.Generic;

namespace CerbiStream.Services;

/// <summary>
/// Transforms log entries to ScoringEventDto format.
/// 
/// IMPORTANT: This transformer does NOT compute scores.
/// - Violations are extracted from GovernanceRuntimeAdapter results
/// - Severities come from cerbi_governance.json via the validator
/// - Score computation happens in the Scoring API
/// 
/// TenantId fallback chain:
/// 1. Governance config file (cerbi_governance.json) - "TenantId" field
/// 2. CerbiStreamOptions.WithTenantId() - code configuration
/// 3. Log entry data - "TenantId" field
/// 4. "unknown" - default fallback
/// </summary>
public static class ScoringEventTransformer
{
    /// <summary>
    /// Transforms a log entry into a ScoringEventDto.
    /// Score is null - computed by the Scoring API.
    /// </summary>
    /// <param name="logEntry">The log entry object.</param>
    /// <param name="logId">Unique log identifier.</param>
    /// <param name="options">CerbiStream configuration options.</param>
    /// <param name="enrichedData">Optional enriched metadata.</param>
    /// <param name="governanceAdapter">Optional governance adapter to read TenantId from config file.</param>
    public static ScoringEventDto Transform(
        object logEntry,
        string logId,
        CerbiStreamOptions options,
        IDictionary<string, object>? enrichedData = null,
        GovernanceRuntimeAdapter? governanceAdapter = null)
    {
        var data = ExtractAsDictionary(logEntry, enrichedData);
        var violations = ExtractViolations(data);

        // TenantId fallback chain: config file -> options -> data -> "unknown"
        var tenantId = governanceAdapter?.GetTenantId()
            ?? options.TenantId
            ?? ExtractString(data, "TenantId")
            ?? "unknown";

        // Stamp ProfileName/AppName onto each ViolationDto for downstream linkage
        var profileName = options.GovernanceProfileName
            ?? ExtractString(data, "GovernanceProfileUsed")
            ?? "default";
        var appName = options.ServiceName
            ?? ExtractString(data, "ApplicationName")
            ?? "unknown";
        for (var i = 0; i < violations.Count; i++)
        {
            var v = violations[i];
            violations[i] = new ViolationDto
            {
                RuleId = v.RuleId,
                Code = v.Code,
                Field = v.Field,
                Severity = v.Severity,
                Message = v.Message,
                Description = v.Description,
                ProfileName = v.ProfileName ?? profileName,
                AppName = v.AppName ?? appName
            };
        }

        return new ScoringEventDto
        {
            SchemaVersion = ContractVersions.ScoringEventSchemaVersion,
            TenantId = tenantId,
            AppName = appName,
            ServiceName = options.ServiceName,
            Environment = ExtractString(data, "Environment") ?? EnvironmentDetector.Environment,
            Runtime = "dotnet",
            LogId = logId,
            CorrelationId = ExtractString(data, "CorrelationId") ?? ExtractString(data, "RequestId"),
            TimestampUtc = DateTime.UtcNow,
            GovernanceProfile = profileName,
            GovernanceMode = ExtractString(data, "GovernanceMode") ?? "Strict",
            LogLevel = ExtractString(data, "LogLevel") ?? "Information",

            // Score is NULL - Scoring API computes it using governance config
            Score = null,

            // Violations with severities from governance config
            Violations = violations,

            GovernanceFlags = new GovernanceFlagsDto
            {
                GovernanceRelaxed = ExtractBool(data, "GovernanceRelaxed")
            },

            // v1.1 identity fields - auto-detected
            AppVersion = EnvironmentDetector.AppVersion,
            InstanceId = EnvironmentDetector.InstanceId,
            DeploymentId = System.Environment.GetEnvironmentVariable("DEPLOYMENT_ID")
        };
    }

    /// <summary>
    /// Extracts violations from GovernanceRuntimeAdapter results.
    /// Severities are already set by the governance validator from cerbi_governance.json.
    /// </summary>
    private static List<ViolationDto> ExtractViolations(IDictionary<string, object> data)
    {
        var result = new List<ViolationDto>();

        if (!data.TryGetValue("GovernanceViolations", out var rawViolations) || rawViolations == null)
            return result;

        if (rawViolations is IEnumerable<object> enumerable)
        {
            foreach (var item in enumerable)
            {
                var violation = MapToViolationDto(item);
                if (violation != null)
                    result.Add(violation);
            }
        }

        return result;
    }

    private static ViolationDto? MapToViolationDto(object item)
    {
        // Handle dictionary format
        if (item is IDictionary<string, object> dict)
        {
            return new ViolationDto
            {
                RuleId = ExtractString(dict, "RuleId"),
                Code = ExtractString(dict, "Code"),
                Field = ExtractString(dict, "Field"),
                // Severity from governance config - NOT hardcoded
                Severity = ExtractString(dict, "Severity"),
                Message = ExtractString(dict, "Message")
            };
        }

        // Handle Cerbi.Governance.GovernanceViolation type via reflection
        var type = item.GetType();
        if (type.Name == "GovernanceViolation")
        {
            return new ViolationDto
            {
                RuleId = GetPropertyValue(item, "RuleId"),
                Code = GetPropertyValue(item, "Code"),
                Field = GetPropertyValue(item, "Field"),
                Severity = GetPropertyValue(item, "Severity"),
                Message = GetPropertyValue(item, "Message")
            };
        }

        return null;
    }

    private static string? GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj)?.ToString();
    }

    private static IDictionary<string, object> ExtractAsDictionary(
        object logEntry,
        IDictionary<string, object>? enrichedData)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (enrichedData != null)
        {
            foreach (var kvp in enrichedData)
                result[kvp.Key] = kvp.Value;
        }

        if (logEntry is IDictionary<string, object> dict)
        {
            foreach (var kvp in dict)
                result[kvp.Key] = kvp.Value;
        }
        else
        {
            foreach (var prop in logEntry.GetType().GetProperties())
            {
                try
                {
                    var value = prop.GetValue(logEntry);
                    if (value != null)
                        result[prop.Name] = value;
                }
                catch { }
            }
        }

        // Flatten nested "Metadata" dictionary to make fields accessible
        if (result.TryGetValue("Metadata", out var metadataObj) && 
            metadataObj is IDictionary<string, object> metadata)
        {
            foreach (var kvp in metadata)
            {
                // Don't overwrite existing top-level keys
                if (!result.ContainsKey(kvp.Key))
                    result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private static string? ExtractString(IDictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static bool ExtractBool(IDictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var value))
        {
            if (value is bool b) return b;
            if (bool.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return false;
    }
}
