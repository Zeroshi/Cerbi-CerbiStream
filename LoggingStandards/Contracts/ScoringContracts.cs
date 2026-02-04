using System;
using System.Collections.Generic;

namespace Cerbi.Contracts
{
    /// <summary>
    /// Contract version constants for schema versioning.
    /// </summary>
    public static class ContractVersions
    {
        public const string ScoringEventSchemaVersion = "1.1";
    }
}

namespace Cerbi.Contracts.Scoring
{
    /// <summary>
    /// DTO representing a scoring event sent from CerbiStream to the Scoring API.
    /// Score is computed by the Scoring API, not the SDK.
    /// </summary>
    public class ScoringEventDto
    {
        /// <summary>
        /// Schema version for backwards compatibility.
        /// </summary>
        public string SchemaVersion { get; set; } = ContractVersions.ScoringEventSchemaVersion;

        /// <summary>
        /// Tenant identifier for multi-tenant support.
        /// </summary>
        public string TenantId { get; set; } = "unknown";

        /// <summary>
        /// Application name that generated this event.
        /// </summary>
        public string AppName { get; set; } = "unknown";

        /// <summary>
        /// Microservice name within the app (optional).
        /// </summary>
        public string? ServiceName { get; set; }

        /// <summary>
        /// Environment (e.g., Development, Staging, Production).
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Runtime identifier (dotnet, java, node, python).
        /// </summary>
        public string Runtime { get; set; } = "dotnet";

        /// <summary>
        /// Unique identifier for this log entry.
        /// </summary>
        public string LogId { get; set; } = string.Empty;

        /// <summary>
        /// Correlation ID for distributed tracing.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// UTC timestamp when the event was created.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Governance profile used for validation.
        /// </summary>
        public string GovernanceProfile { get; set; } = "default";

        /// <summary>
        /// Governance mode (Strict, Relaxed, Monitor).
        /// </summary>
        public string GovernanceMode { get; set; } = "Strict";

        /// <summary>
        /// Log level of the original message.
        /// </summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// Score breakdown. NULL when sent from SDK - computed by Scoring API.
        /// </summary>
        public ScoreBreakdownDto? Score { get; set; }

        /// <summary>
        /// List of governance violations detected.
        /// </summary>
        public List<ViolationDto>? Violations { get; set; }

        /// <summary>
        /// Governance flags for special handling.
        /// </summary>
        public GovernanceFlagsDto? GovernanceFlags { get; set; }

        /// <summary>
        /// Raw payload data from the original log entry.
        /// </summary>
        public IDictionary<string, object>? RawPayload { get; set; }

        // v1.1 identity fields

        /// <summary>
        /// Application version (e.g., 2.1.0). Auto-detected from assembly.
        /// </summary>
        public string? AppVersion { get; set; }

        /// <summary>
        /// Instance/pod/container identifier. Auto-detected from HOSTNAME.
        /// </summary>
        public string? InstanceId { get; set; }

        /// <summary>
        /// Deployment/release identifier from CI/CD.
        /// </summary>
        public string? DeploymentId { get; set; }
    }

    /// <summary>
    /// Score breakdown by category.
    /// </summary>
    public class ScoreBreakdownDto
    {
        /// <summary>
        /// Overall score (0-100).
        /// </summary>
        public int Overall { get; set; }

        /// <summary>
        /// Governance compliance score (0-100).
        /// </summary>
        public int Governance { get; set; }

        /// <summary>
        /// Safety/security score (0-100).
        /// </summary>
        public int Safety { get; set; }
    }

    /// <summary>
    /// Represents a single governance violation.
    /// </summary>
    public class ViolationDto
    {
        /// <summary>
        /// Rule identifier that was violated.
        /// </summary>
        public string? RuleId { get; set; }

        /// <summary>
        /// Violation code/category.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Field that caused the violation.
        /// </summary>
        public string? Field { get; set; }

        /// <summary>
        /// Severity from governance config (Critical, Error, Warning, Info).
        /// </summary>
        public string? Severity { get; set; }

        /// <summary>
        /// Human-readable violation message.
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// Governance-related flags.
    /// </summary>
    public class GovernanceFlagsDto
    {
        /// <summary>
        /// Whether governance was relaxed for this entry.
        /// </summary>
        public bool GovernanceRelaxed { get; set; }
    }
}
