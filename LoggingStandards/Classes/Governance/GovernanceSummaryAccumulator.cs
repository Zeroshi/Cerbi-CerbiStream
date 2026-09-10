extern alias GovernanceCore;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RuntimeGovernanceViolation = GovernanceCore::Cerbi.Governance.GovernanceViolation;

namespace CerbiStream.GovernanceRuntime.Governance;

public class GovernanceSummaryOptions
{
    public string? TenantId { get; set; }
    public string RuntimeId { get; set; } = "cerbi-dotnet-mel";
    public string RuntimeVersion { get; set; } = "2.0.6";
    public string Source { get; set; } = "dotnet-mel";
    public string AppName { get; set; } = "unknown";
    public string Environment { get; set; } = "unknown";
    public string? ServiceName { get; set; }
    public string Runtime { get; set; } = "dotnet-mel";
    public string? EmitterId { get; set; }
    public string? StreamId { get; set; }
    public string PeriodType { get; set; } = "minute";
    public string? GovernanceProfile { get; set; }
    public string? GovernanceProfileId { get; set; }
    public string? GovernanceProfileVersion { get; set; }
    public string? GovernanceProfileHash { get; set; }
    public TimeSpan Retention { get; set; } = TimeSpan.FromMinutes(2);
    public Func<DateTimeOffset> Now { get; set; } = () => DateTimeOffset.UtcNow;
}

public sealed record GovernanceSummaryEvent(string Outcome, IReadOnlyList<GovernanceSummaryViolation> Violations);

public sealed record GovernanceSummaryViolation(string RuleId, string Action, string Severity);

public sealed class GovernanceSummaryBatch
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = "1.0";

    [JsonPropertyName("tenantId")]
    public string TenantId { get; init; } = string.Empty;

    [JsonPropertyName("runtimeId")]
    public string RuntimeId { get; init; } = string.Empty;

    [JsonPropertyName("runtimeVersion")]
    public string RuntimeVersion { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; init; }

    [JsonPropertyName("summaries")]
    public IReadOnlyList<GovernanceSummarySnapshot> Summaries { get; init; } = Array.Empty<GovernanceSummarySnapshot>();
}

public sealed class GovernanceSummarySnapshot
{
    [JsonPropertyName("summaryId")]
    public string SummaryId { get; init; } = string.Empty;

    [JsonPropertyName("appName")]
    public string AppName { get; init; } = string.Empty;

    [JsonPropertyName("environment")]
    public string Environment { get; init; } = string.Empty;

    [JsonPropertyName("serviceName")]
    public string ServiceName { get; init; } = string.Empty;

    [JsonPropertyName("runtime")]
    public string Runtime { get; init; } = string.Empty;

    [JsonPropertyName("emitterId")]
    public string EmitterId { get; init; } = string.Empty;

    [JsonPropertyName("streamId")]
    public string StreamId { get; init; } = string.Empty;

    [JsonPropertyName("windowStartUtc")]
    public DateTimeOffset WindowStartUtc { get; init; }

    [JsonPropertyName("windowEndUtc")]
    public DateTimeOffset WindowEndUtc { get; init; }

    [JsonPropertyName("periodType")]
    public string PeriodType { get; init; } = "minute";

    [JsonPropertyName("governanceProfile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GovernanceProfile { get; init; }

    [JsonPropertyName("governanceProfileId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GovernanceProfileId { get; init; }

    [JsonPropertyName("governanceProfileVersion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GovernanceProfileVersion { get; init; }

    [JsonPropertyName("governanceProfileHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GovernanceProfileHash { get; init; }

    [JsonPropertyName("counters")]
    public IReadOnlyDictionary<string, long> Counters { get; init; } = new Dictionary<string, long>();

    [JsonPropertyName("ruleCounts")]
    public IReadOnlyList<GovernanceSummaryRuleCount> RuleCounts { get; init; } = Array.Empty<GovernanceSummaryRuleCount>();
}

public sealed class GovernanceSummaryRuleCount
{
    [JsonPropertyName("ruleId")]
    public string RuleId { get; init; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public long Count { get; init; }
}

public interface IGovernanceSummarySink
{
    void Record(IDictionary<string, object> fields, DateTimeOffset? at = null);
}

public sealed class GovernanceSummaryAccumulator
{
    private readonly GovernanceSummaryOptions _options;
    private readonly ConcurrentDictionary<string, MutableSummary> _summaries = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public GovernanceSummaryAccumulator(GovernanceSummaryOptions? options)
    {
        _options = Normalize(options ?? new GovernanceSummaryOptions());
    }

    public void Record(GovernanceSummaryEvent ev, DateTimeOffset? at = null)
    {
        var effectiveAt = (at ?? _options.Now()).ToUniversalTime();
        lock (_lock)
        {
            var window = GetWindow(effectiveAt, _options.PeriodType);
            var summary = _summaries.GetOrAdd(
                BuildSummaryId(_options, window.Start),
                id => new MutableSummary(id, _options, window));
            summary.Dirty = true;
            Increment(summary.Counters, "received", 1);
            Increment(summary.Counters, "evaluated", 1);
            var outcome = string.IsNullOrWhiteSpace(ev.Outcome) ? "passed" : ev.Outcome;
            Increment(summary.Counters, outcome.Equals("error", StringComparison.OrdinalIgnoreCase) ? "errors" : outcome, 1);
            var violations = ev.Violations ?? Array.Empty<GovernanceSummaryViolation>();
            Increment(summary.Counters, "violations", violations.Count);
            if (!outcome.Equals("passed", StringComparison.OrdinalIgnoreCase))
                Increment(summary.Counters, "detailedEvidence", 1);

            foreach (var violation in violations)
            {
                var key = $"{violation.RuleId}|{violation.Action}|{violation.Severity}";
                summary.RuleCounts.AddOrUpdate(
                    key,
                    _ => new GovernanceSummaryRuleCount
                    {
                        RuleId = violation.RuleId,
                        Action = violation.Action,
                        Severity = violation.Severity,
                        Count = 1
                    },
                    (_, existing) => new GovernanceSummaryRuleCount
                    {
                        RuleId = existing.RuleId,
                        Action = existing.Action,
                        Severity = existing.Severity,
                        Count = existing.Count + 1
                    });
            }
        }
    }

    public void RecordFields(IDictionary<string, object> fields, DateTimeOffset? at = null)
        => Record(EventFromFields(fields), at);

    public GovernanceSummaryBatch? Snapshot()
    {
        lock (_lock)
        {
            PruneCleanExpired();
            var emitted = _summaries.Values
                .Where(summary => summary.Dirty)
                .Select(summary => summary.ToWire())
                .OrderBy(summary => summary.SummaryId, StringComparer.Ordinal)
                .ToArray();
            if (emitted.Length == 0)
                return null;

            return new GovernanceSummaryBatch
            {
                TenantId = _options.TenantId ?? string.Empty,
                RuntimeId = _options.RuntimeId,
                RuntimeVersion = _options.RuntimeVersion,
                Source = _options.Source,
                GeneratedAtUtc = _options.Now().ToUniversalTime(),
                Summaries = emitted
            };
        }
    }

    public GovernanceSummaryBatch? Flush() => Snapshot();

    public void MarkFlushed(GovernanceSummaryBatch? batch)
    {
        if (batch is null)
            return;

        lock (_lock)
        {
            foreach (var sent in batch.Summaries)
            {
                if (!_summaries.TryGetValue(sent.SummaryId, out var current))
                    continue;

                var wire = current.ToWire();
                if (CountersEqual(wire.Counters, sent.Counters) && RuleCountsEqual(wire.RuleCounts, sent.RuleCounts))
                    current.Dirty = false;
            }
            PruneCleanExpired();
        }
    }

    public static GovernanceSummaryEvent EventFromFields(IDictionary<string, object> fields)
    {
        var relaxed = ExtractBool(fields, "GovernanceRelaxed");
        var decision = ExtractString(fields, "GovernanceDecision")?.ToLowerInvariant();
        var action = ExtractString(fields, "EnforcementAction")?.ToLowerInvariant();
        var mode = ExtractString(fields, "GovernanceMode")?.ToLowerInvariant();
        var defaultViolationAction = action switch
        {
            "redact" or "redacted" or "mask" or "masked" => "redacted",
            "block" or "blocked" or "deny" or "denied" => "blocked",
            _ => null
        };
        var violations = ExtractViolations(fields, defaultViolationAction).ToArray();

        if (mode is "unknown" or "invalid" || decision == "error" || action == "error")
            return new GovernanceSummaryEvent("error", violations);
        if (relaxed || decision == "relaxed" || mode is "relax" or "relaxed")
            return new GovernanceSummaryEvent("relaxed", violations);
        if (action is "block" or "blocked" or "deny" or "denied" || decision == "blocked")
            return new GovernanceSummaryEvent("blocked", violations);
        if (action is "redact" or "redacted" or "mask" || decision == "redacted" || violations.Any(v => v.Action == "redacted"))
            return new GovernanceSummaryEvent("redacted", violations);
        if (violations.Length > 0 || decision is "warned" or "violating")
            return new GovernanceSummaryEvent("violating", violations);
        return new GovernanceSummaryEvent("passed", Array.Empty<GovernanceSummaryViolation>());
    }

    private void PruneCleanExpired()
    {
        var now = _options.Now().ToUniversalTime();
        foreach (var entry in _summaries.ToArray())
        {
            if (!entry.Value.Dirty && entry.Value.WindowEndUtc.Add(_options.Retention) <= now)
                _summaries.TryRemove(entry.Key, out _);
        }
    }

    private static IEnumerable<GovernanceSummaryViolation> ExtractViolations(IDictionary<string, object> fields, string? defaultAction = null)
    {
        if (!fields.TryGetValue("GovernanceViolations", out var raw) || raw is null)
            yield break;

        if (raw is string text)
        {
            foreach (var violation in ParseViolationString(text))
                yield return WithDefaultAction(violation, defaultAction);
            yield break;
        }

        if (raw is JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                yield break;
            foreach (var item in element.EnumerateArray())
            {
                var mapped = MapJsonViolation(item);
                if (mapped is not null)
                    yield return WithDefaultAction(mapped, defaultAction);
            }
            yield break;
        }

        if (raw is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is null)
                    continue;
                var mapped = MapViolationObject(item);
                if (mapped is not null)
                    yield return WithDefaultAction(mapped, defaultAction);
            }
        }
    }

    private static IReadOnlyList<GovernanceSummaryViolation> ParseViolationString(string text)
    {
        var result = new List<GovernanceSummaryViolation>();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var mapped = MapJsonViolation(item);
                    if (mapped is not null)
                        result.Add(mapped);
                }
                return result;
            }
        }
        catch
        {
            // Fall through to comma-delimited rule IDs.
        }

        foreach (var ruleId in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            result.Add(new GovernanceSummaryViolation(ruleId, "audit", SeverityForRule(ruleId)));

        return result;
    }

    private static GovernanceSummaryViolation? MapViolationObject(object item)
    {
        if (item is RuntimeGovernanceViolation runtimeViolation)
        {
            var ruleId = FirstNonBlank(runtimeViolation.RuleId, runtimeViolation.Code, runtimeViolation.Field, "unknown");
            return new GovernanceSummaryViolation(ruleId, "audit", NormalizeSeverity(runtimeViolation.Severity));
        }

        if (item is IDictionary<string, object> dict)
        {
            var ruleId = FirstNonBlank(
                ExtractString(dict, "RuleId"),
                ExtractString(dict, "ruleId"),
                ExtractString(dict, "Code"),
                ExtractString(dict, "Field"),
                "unknown");
            var action = FirstNonBlank(ExtractString(dict, "Action"), ExtractString(dict, "action"), "audit");
            var severity = FirstNonBlank(ExtractString(dict, "Severity"), ExtractString(dict, "severity"), SeverityForRule(ruleId));
            return new GovernanceSummaryViolation(ruleId, NormalizeAction(action), NormalizeSeverity(severity));
        }

        var type = item.GetType();
        var reflectedRuleId = FirstNonBlank(
            type.GetProperty("RuleId")?.GetValue(item)?.ToString(),
            type.GetProperty("ruleId")?.GetValue(item)?.ToString(),
            type.GetProperty("Code")?.GetValue(item)?.ToString(),
            type.GetProperty("Field")?.GetValue(item)?.ToString(),
            "unknown");
        var reflectedAction = FirstNonBlank(type.GetProperty("Action")?.GetValue(item)?.ToString(), "audit");
        var reflectedSeverity = FirstNonBlank(type.GetProperty("Severity")?.GetValue(item)?.ToString(), SeverityForRule(reflectedRuleId));
        return new GovernanceSummaryViolation(reflectedRuleId, NormalizeAction(reflectedAction), NormalizeSeverity(reflectedSeverity));
    }

    private static GovernanceSummaryViolation? MapJsonViolation(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return null;

        var ruleId = FirstNonBlank(
            JsonString(item, "RuleId"),
            JsonString(item, "ruleId"),
            JsonString(item, "Code"),
            JsonString(item, "Field"),
            "unknown");
        var action = FirstNonBlank(JsonString(item, "Action"), JsonString(item, "action"), "audit");
        var severity = FirstNonBlank(JsonString(item, "Severity"), JsonString(item, "severity"), SeverityForRule(ruleId));
        return new GovernanceSummaryViolation(ruleId, NormalizeAction(action), NormalizeSeverity(severity));
    }

    private static GovernanceSummaryViolation WithDefaultAction(GovernanceSummaryViolation violation, string? defaultAction)
    {
        if (string.IsNullOrWhiteSpace(defaultAction) || !string.Equals(violation.Action, "audit", StringComparison.OrdinalIgnoreCase))
            return violation;
        return violation with { Action = defaultAction };
    }

    private static bool CountersEqual(IReadOnlyDictionary<string, long> left, IReadOnlyDictionary<string, long> right)
        => left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var value) && value == pair.Value);

    private static bool RuleCountsEqual(IReadOnlyList<GovernanceSummaryRuleCount> left, IReadOnlyList<GovernanceSummaryRuleCount> right)
    {
        if (left.Count != right.Count)
            return false;

        var orderedLeft = left.OrderBy(RuleKey, StringComparer.Ordinal).ToArray();
        var orderedRight = right.OrderBy(RuleKey, StringComparer.Ordinal).ToArray();
        for (var i = 0; i < orderedLeft.Length; i++)
        {
            if (RuleKey(orderedLeft[i]) != RuleKey(orderedRight[i]) || orderedLeft[i].Count != orderedRight[i].Count)
                return false;
        }
        return true;
    }

    private static string RuleKey(GovernanceSummaryRuleCount rule) => $"{rule.RuleId}|{rule.Action}|{rule.Severity}";

    private static void Increment(IDictionary<string, long> counters, string key, long amount)
        => counters[key] = counters.TryGetValue(key, out var value) ? value + amount : amount;

    private static GovernanceSummaryOptions Normalize(GovernanceSummaryOptions options)
    {
        options.ServiceName = FirstNonBlank(options.ServiceName, options.AppName);
        options.EmitterId = FirstNonBlank(options.EmitterId, DefaultEmitterId());
        options.StreamId = FirstNonBlank(options.StreamId, DefaultStreamId(options.EmitterId));
        options.PeriodType = FirstNonBlank(options.PeriodType, "minute");
        options.TenantId = FirstNonBlank(options.TenantId, "unknown-tenant");
        options.AppName = FirstNonBlank(options.AppName, "unknown");
        options.Environment = FirstNonBlank(options.Environment, "unknown");
        return options;
    }

    private static (DateTimeOffset Start, DateTimeOffset End) GetWindow(DateTimeOffset at, string periodType)
    {
        var utc = at.ToUniversalTime();
        var start = periodType switch
        {
            "hourly" => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero),
            "daily" => new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero),
            _ => new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, TimeSpan.Zero)
        };
        var end = periodType switch
        {
            "hourly" => start.AddHours(1),
            "daily" => start.AddDays(1),
            _ => start.AddMinutes(1)
        };
        return (start, end);
    }

    private static string BuildSummaryId(GovernanceSummaryOptions options, DateTimeOffset windowStart)
    {
        var profile = FirstNonBlank(options.GovernanceProfileId, options.GovernanceProfile, "default");
        var version = FirstNonBlank(options.GovernanceProfileVersion, "unknown");
        var hash = FirstNonBlank(options.GovernanceProfileHash, Sha256Hex($"{profile}|{version}"));
        return string.Join("|",
            FirstNonBlank(options.TenantId, "unknown-tenant"),
            FirstNonBlank(options.AppName, "unknown"),
            FirstNonBlank(options.Environment, "unknown"),
            FirstNonBlank(options.Runtime, options.RuntimeId),
            FirstNonBlank(options.EmitterId, "unknown-emitter"),
            FirstNonBlank(options.StreamId, "unknown-stream"),
            windowStart.UtcDateTime.ToString("O"),
            FirstNonBlank(options.PeriodType, "minute"),
            profile,
            version,
            hash);
    }

    private static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string DefaultEmitterId()
    {
        var keys = new[] { "CERBI_EMITTER_ID", "CONTAINER_APP_REPLICA_NAME", "WEBSITE_INSTANCE_ID", "HOSTNAME", "COMPUTERNAME" };
        foreach (var key in keys)
        {
            var value = System.Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                return value!;
        }

        try { return Dns.GetHostName(); }
        catch { return $"pid-{System.Environment.ProcessId}"; }
    }

    private static string DefaultStreamId(string? emitterId)
        => $"{FirstNonBlank(emitterId, "unknown-emitter")}:pid-{System.Environment.ProcessId}:{DateTimeOffset.UtcNow:O}:{Guid.NewGuid():N}";

    private static string FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value!;
        }
        return string.Empty;
    }

    private static string? ExtractString(IDictionary<string, object> values, string key)
    {
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                return pair.Value?.ToString();
        }
        return null;
    }

    private static bool ExtractBool(IDictionary<string, object> values, string key)
    {
        var value = ExtractString(values, key);
        return bool.TryParse(value, out var parsed) && parsed;
    }

    private static string? JsonString(JsonElement element, string key)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                return property.Value.ToString();
        }
        return null;
    }

    private static string SeverityForRule(string? ruleId)
        => ruleId?.Contains("Critical", StringComparison.OrdinalIgnoreCase) == true ? "critical" : "warning";

    private static string NormalizeAction(string? action)
    {
        var lower = string.IsNullOrWhiteSpace(action) ? "audit" : action!.ToLowerInvariant();
        return lower switch
        {
            "redact" or "mask" or "masked" or "remove" or "removed" => "redacted",
            "block" or "deny" or "denied" => "blocked",
            _ => lower
        };
    }

    private static string NormalizeSeverity(string? severity)
    {
        var value = string.IsNullOrWhiteSpace(severity) ? "warning" : severity!.ToLowerInvariant();
        return value == "error" ? "warning" : value;
    }

    private sealed class MutableSummary
    {
        public MutableSummary(string summaryId, GovernanceSummaryOptions options, (DateTimeOffset Start, DateTimeOffset End) window)
        {
            SummaryId = summaryId;
            AppName = options.AppName;
            Environment = options.Environment;
            ServiceName = options.ServiceName ?? options.AppName;
            Runtime = options.Runtime;
            EmitterId = options.EmitterId ?? string.Empty;
            StreamId = options.StreamId ?? string.Empty;
            WindowStartUtc = window.Start;
            WindowEndUtc = window.End;
            PeriodType = options.PeriodType;
            GovernanceProfile = options.GovernanceProfile;
            GovernanceProfileId = options.GovernanceProfileId;
            GovernanceProfileVersion = options.GovernanceProfileVersion;
            GovernanceProfileHash = options.GovernanceProfileHash;
            foreach (var key in new[] { "received", "evaluated", "passed", "violating", "redacted", "blocked", "relaxed", "errors", "violations", "detailedEvidence" })
                Counters[key] = 0;
        }

        public string SummaryId { get; }
        public string AppName { get; }
        public string Environment { get; }
        public string ServiceName { get; }
        public string Runtime { get; }
        public string EmitterId { get; }
        public string StreamId { get; }
        public DateTimeOffset WindowStartUtc { get; }
        public DateTimeOffset WindowEndUtc { get; }
        public string PeriodType { get; }
        public string? GovernanceProfile { get; }
        public string? GovernanceProfileId { get; }
        public string? GovernanceProfileVersion { get; }
        public string? GovernanceProfileHash { get; }
        public Dictionary<string, long> Counters { get; } = new(StringComparer.Ordinal);
        public ConcurrentDictionary<string, GovernanceSummaryRuleCount> RuleCounts { get; } = new(StringComparer.Ordinal);
        public bool Dirty { get; set; } = true;

        public GovernanceSummarySnapshot ToWire() => new()
        {
            SummaryId = SummaryId,
            AppName = AppName,
            Environment = Environment,
            ServiceName = ServiceName,
            Runtime = Runtime,
            EmitterId = EmitterId,
            StreamId = StreamId,
            WindowStartUtc = WindowStartUtc,
            WindowEndUtc = WindowEndUtc,
            PeriodType = PeriodType,
            GovernanceProfile = GovernanceProfile,
            GovernanceProfileId = GovernanceProfileId,
            GovernanceProfileVersion = GovernanceProfileVersion,
            GovernanceProfileHash = GovernanceProfileHash,
            Counters = new Dictionary<string, long>(Counters, StringComparer.Ordinal),
            RuleCounts = RuleCounts.Values.OrderBy(RuleKey, StringComparer.Ordinal).ToArray()
        };
    }
}
