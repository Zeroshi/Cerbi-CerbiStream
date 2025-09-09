using Cerbi.Governance;               // RuntimeGovernanceValidator, IRuntimeGovernanceSource, FileGovernanceSource
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CerbiStream.GovernanceRuntime.Governance;

/// <summary>
/// Calls the runtime validator to tag the payload (GovernanceViolations, GovernanceRelaxed, etc.)
/// and then REDACTS any fields that show up as Forbidden/Disallowed in the violations list.
/// </summary>
public sealed class GovernanceRuntimeAdapter
{
    private readonly RuntimeGovernanceValidator _validator;

    /// <param name="profileName">Active profile name (e.g., "default", "Orders").</param>
    /// <param name="configPath">Path to cerbi_governance.json (if null, uses env or ./cerbi_governance.json)</param>
    public GovernanceRuntimeAdapter(string profileName, string? configPath = null)
    {
        var appProfile = string.IsNullOrWhiteSpace(profileName) ? "default" : profileName;

        // Resolve a concrete path for the file source
        var path = !string.IsNullOrWhiteSpace(configPath)
            ? configPath!
            : (Environment.GetEnvironmentVariable("CERBI_GOVERNANCE_PATH")
               ?? Path.Combine(AppContext.BaseDirectory, "cerbi_governance.json"));

        IRuntimeGovernanceSource source = new FileGovernanceSource(path);

        // ✅ Correct ctor: (isEnabled, profileName, source)
        _validator = new RuntimeGovernanceValidator(
            isEnabled: () => true,
            profileName: appProfile,
            source: source);
    }

    public void ValidateAndRedactInPlace(IDictionary<string, object> data)
    {
        // Respect Relax tag if caller set it (runtime also recognizes this)
        if (IsRelaxed(data))
        {
            data["GovernanceRelaxed"] = true;
            return;
        }

        // The runtime expects a concrete Dictionary<string, object>.
        var working = AsDictionary(data);

        // 1) Let the runtime mutate tags into the payload
        _validator.ValidateInPlace(working);

        // 2) Inspect violations and compute fields to redact
        var toRedact = GetFieldsToRedactFromViolations(working).ToArray();

        // 3) Apply redaction to the working dictionary
        foreach (var field in toRedact)
            RedactIfPresent(working, field);

        // 4) Propagate changes back into the original IDictionary
        CopyInto(working, data);
    }

    public void ValidateAndRedactInPlace(JsonElement json)
        => ValidateAndRedactInPlace(ToDictionary(json));

    private static bool IsRelaxed(IDictionary<string, object> data)
        => data.TryGetValue("GovernanceRelaxed", out var v) && v is true;

    private static void RedactIfPresent(IDictionary<string, object> data, string field)
    {
        var hit = data.Keys.FirstOrDefault(k => string.Equals(k, field, StringComparison.OrdinalIgnoreCase));
        if (hit is not null)
            data[hit] = "***REDACTED***";
    }

    private static IEnumerable<string> GetFieldsToRedactFromViolations(IDictionary<string, object> data)
    {
        if (!data.TryGetValue("GovernanceViolations", out var v) || v is null)
            yield break;

        // (1) IEnumerable<object> form
        if (v is IEnumerable<object> list)
        {
            foreach (var item in list)
            {
                var field = TryGetViolationField(item);
                if (field is not null) yield return field;
            }
            yield break;
        }

        // (2) JsonElement array form
        if (v is JsonElement el && el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                var field = TryGetViolationField(item);
                if (field is not null) yield return field;
            }
            yield break;
        }

        // (3) JSON string form (cannot yield inside try/catch — collect first)
        if (v is string s)
        {
            var fields = new List<string>();
            try
            {
                using var doc = JsonDocument.Parse(s);
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var field = TryGetViolationField(item);
                    if (field is not null) fields.Add(field);
                }
            }
            catch { /* ignore malformed JSON */ }

            foreach (var f in fields)
                yield return f;
        }
    }

    private static string? TryGetViolationField(object item)
    {
        // object dictionary
        if (item is IDictionary<string, object> d)
        {
            if (!d.TryGetValue("Code", out var code)) return null;
            if (!IsForbiddenCode(code?.ToString())) return null;
            return d.TryGetValue("Field", out var f) ? f?.ToString() : null;
        }

        // JsonElement
        if (item is JsonElement el && el.ValueKind == JsonValueKind.Object)
        {
            var code = el.TryGetProperty("Code", out var c) ? c.GetString() : null;
            if (!IsForbiddenCode(code)) return null;
            return el.TryGetProperty("Field", out var f) ? f.GetString() : null;
        }

        return null;
    }

    private static bool IsForbiddenCode(string? code) =>
        string.Equals(code, "ForbiddenField", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(code, "DisallowedFieldPresent", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, object> ToDictionary(JsonElement root)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (root.ValueKind != JsonValueKind.Object) return dict;

        foreach (var p in root.EnumerateObject())
        {
            dict[p.Name] = p.Value.ValueKind switch
            {
                JsonValueKind.String => (object)(p.Value.GetString()!),
                JsonValueKind.Number => p.Value.TryGetInt64(out var i) ? i : p.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null or JsonValueKind.Undefined => null!,
                _ => p.Value.ToString()
            };
        }
        return dict;
    }

    private static Dictionary<string, object> AsDictionary(IDictionary<string, object> source) =>
        source as Dictionary<string, object> ?? new Dictionary<string, object>(source, StringComparer.OrdinalIgnoreCase);

    private static void CopyInto(Dictionary<string, object> from, IDictionary<string, object> to)
    {
        foreach (var kv in from)
            to[kv.Key] = kv.Value;
    }
}
