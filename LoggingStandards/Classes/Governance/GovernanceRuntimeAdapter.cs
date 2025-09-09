using Cerbi.Governance;               // RuntimeGovernanceValidator, IRuntimeGovernanceSource, FileGovernanceSource
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CerbiStream.GovernanceRuntime.Governance;

/// <summary>
/// Validates payloads in-place (tags GovernanceViolations / GovernanceRelaxed) and
/// REDACTS fields that are forbidden/disallowed by either runtime violations or by policy in the profile file.
/// </summary>
public sealed class GovernanceRuntimeAdapter
{
    private readonly RuntimeGovernanceValidator _validator;
    private readonly string _profileName;
    private readonly string _configPath;

    // simple cache for parsed policy
    private DateTime _lastLoadedUtc;
    private HashSet<string> _policyRedactFields = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="profileName">Active profile name (e.g., "default", "Orders").</param>
    /// <param name="configPath">Path to cerbi_governance.json; if null, uses env CERBI_GOVERNANCE_PATH or ./cerbi_governance.json.</param>
    public GovernanceRuntimeAdapter(string profileName, string? configPath = null)
    {
        _profileName = string.IsNullOrWhiteSpace(profileName) ? "default" : profileName;
        _configPath = !string.IsNullOrWhiteSpace(configPath)
            ? configPath!
            : (Environment.GetEnvironmentVariable("CERBI_GOVERNANCE_PATH")
               ?? Path.Combine(AppContext.BaseDirectory, "cerbi_governance.json"));

        IRuntimeGovernanceSource source = new FileGovernanceSource(_configPath);

        // ctor: (isEnabled, profileName, source)
        _validator = new RuntimeGovernanceValidator(
            isEnabled: () => true,
            profileName: _profileName,
            source: source);
    }

    public void ValidateAndRedactInPlace(IDictionary<string, object> data)
    {
        // 0) Respect Relax tag (bypass checks + redaction)
        if (IsRelaxed(data))
        {
            data["GovernanceRelaxed"] = true;
            return;
        }

        // Runtime expects a concrete Dictionary<string, object>
        var working = AsDictionary(data);

        // 1) Tag using the runtime validator (adds GovernanceViolations[], GovernanceProfileVersion, etc.)
        _validator.ValidateInPlace(working);

        // 2) Compute fields to redact
        var toRedact = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 2a) From runtime violations
        foreach (var f in GetFieldsToRedactFromViolations(working))
            toRedact.Add(f);

        // 2b) From policy file (disallowed + forbidden)
        foreach (var f in GetFieldsToRedactFromPolicy())
            toRedact.Add(f);

        // 3) Apply redaction
        foreach (var field in toRedact)
            RedactIfPresent(working, field);

        // 4) Copy changes back into original IDictionary
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

    private IEnumerable<string> GetFieldsToRedactFromPolicy()
    {
        try
        {
            if (!File.Exists(_configPath))
                return Array.Empty<string>();

            var lastWrite = File.GetLastWriteTimeUtc(_configPath);
            if (_policyRedactFields.Count == 0 || lastWrite > _lastLoadedUtc)
            {
                _policyRedactFields = ParsePolicyRedactFields(_configPath, _profileName);
                _lastLoadedUtc = lastWrite;
            }

            return _policyRedactFields;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static HashSet<string> ParsePolicyRedactFields(string path, string profileName)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        // Find LoggingProfiles (case-insensitive)
        if (!TryGetPropertyCI(root, "LoggingProfiles", out var profilesEl) ||
            profilesEl.ValueKind != JsonValueKind.Object)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Find the target profile (case-insensitive by key), else first
        JsonElement? profileEl = null;
        foreach (var p in profilesEl.EnumerateObject())
        {
            if (string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase))
            {
                profileEl = p.Value;
                break;
            }
        }
        profileEl ??= profilesEl.EnumerateObject().FirstOrDefault().Value;

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (profileEl is null || profileEl.Value.ValueKind != JsonValueKind.Object)
            return result;

        // DisallowedFields
        if (TryGetPropertyCI(profileEl.Value, "DisallowedFields", out var dis) &&
            dis.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in dis.EnumerateArray())
                if (s.ValueKind == JsonValueKind.String)
                    result.Add(s.GetString()!);
        }

        // FieldSeverities: any == "Forbidden"
        if (TryGetPropertyCI(profileEl.Value, "FieldSeverities", out var sev) &&
            sev.ValueKind == JsonValueKind.Object)
        {
            foreach (var kv in sev.EnumerateObject())
            {
                if (kv.Value.ValueKind == JsonValueKind.String &&
                    string.Equals(kv.Value.GetString(), "Forbidden", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(kv.Name);
                }
            }
        }

        return result;
    }

    private static bool TryGetPropertyCI(JsonElement obj, string name, out JsonElement value)
    {
        if (obj.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }
        if (obj.TryGetProperty(name, out value)) return true;
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

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
