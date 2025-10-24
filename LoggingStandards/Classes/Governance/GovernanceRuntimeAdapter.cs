using Cerbi.Governance; // RuntimeGovernanceValidator, IRuntimeGovernanceSource, FileGovernanceSource
using CerbiStream.Observability;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

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
 private readonly object _policyLock = new();

 // File watcher to avoid checking file timestamp on every validation
 private volatile int _policyStale;
 private FileSystemWatcher? _policyWatcher;

 // Simple pool for temporary dictionaries to reduce allocations
 private static readonly ConcurrentBag<Dictionary<string, object>> _dictPool = new();

 // Pool for HashSet<string> used as toRedact to avoid per-call allocations
 private static readonly ConcurrentBag<HashSet<string>> _hashSetPool = new();

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

 TryInitWatcher();
 }

 private void TryInitWatcher()
 {
 try
 {
 var dir = Path.GetDirectoryName(_configPath);
 var name = Path.GetFileName(_configPath);
 if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name)) return;
 if (!Directory.Exists(dir)) return;

 _policyWatcher = new FileSystemWatcher(dir, name)
 {
 NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes
 };
 _policyWatcher.Changed += (_, __) => Interlocked.Exchange(ref _policyStale,1);
 _policyWatcher.Created += (_, __) => Interlocked.Exchange(ref _policyStale,1);
 _policyWatcher.Renamed += (_, __) => Interlocked.Exchange(ref _policyStale,1);
 _policyWatcher.EnableRaisingEvents = true;
 }
 catch
 {
 // ignore watcher failures — fall back to timestamp checks
 _policyWatcher = null;
 }
 }

 public void ValidateAndRedactInPlace(IDictionary<string, object> data)
 {
 Metrics.IncrementLogsProcessed();
 //0) Respect Relax tag (bypass checks + redaction)
 if (IsRelaxed(data))
 {
 data["GovernanceRelaxed"] = true;
 return;
 }

 // Runtime expects a concrete Dictionary<string, object>
 var working = AsDictionary(data, out var rentedDict);

 // Rent a HashSet for toRedact to avoid per-call allocation
 var toRedact = RentHashSet();
 try
 {
 //1) Tag using the runtime validator (adds GovernanceViolations[], GovernanceProfileVersion, etc.)
 _validator.ValidateInPlace(working);

 //2a) From runtime violations
 long violationCount =0;
 foreach (var f in GetFieldsToRedactFromViolations(working))
 {
 toRedact.Add(f);
 violationCount++;
 }
 if (violationCount >0) Metrics.IncrementViolations(violationCount);

 //2b) From policy file (disallowed + forbidden)
 foreach (var f in GetFieldsToRedactFromPolicy())
 toRedact.Add(f);

 //3) Apply redaction
 long redacted =0;
 foreach (var field in toRedact)
 {
 if (RedactIfPresentAndCount(working, field)) redacted++;
 }
 if (redacted >0) Metrics.IncrementRedactions(redacted);

 //4) Copy changes back into original IDictionary if a different instance was created
 if (!ReferenceEquals(working, data))
 CopyInto(working, data);
 }
 finally
 {
 ReturnHashSet(toRedact);
 if (rentedDict)
 ReturnDictionaryToPool(working);
 }
 }

 public void ValidateAndRedactInPlace(JsonElement json)
 => ValidateAndRedactInPlace(ToDictionary(json));

 private static bool IsRelaxed(IDictionary<string, object> data)
 => data.TryGetValue("GovernanceRelaxed", out var v) && v is true;

 private static void RedactIfPresent(IDictionary<string, object> data, string field)
 {
 // Most callers pass in a Dictionary<string, object> created with StringComparer.OrdinalIgnoreCase
 // so try an O(1) lookup first to avoid enumerating keys repeatedly.
 if (data is Dictionary<string, object> dict)
 {
 if (dict.TryGetValue(field, out var _))
 {
 dict[field] = "***REDACTED***";
 }
 return;
 }

 // Fallback: case-insensitive search
 var hit = data.Keys.FirstOrDefault(k => string.Equals(k, field, StringComparison.OrdinalIgnoreCase));
 if (hit is not null)
 data[hit] = "***REDACTED***";
 }

 private static bool RedactIfPresentAndCount(IDictionary<string, object> data, string field)
 {
 if (data is Dictionary<string, object> dict)
 {
 if (dict.TryGetValue(field, out var _))
 {
 dict[field] = "***REDACTED***";
 return true;
 }
 return false;
 }
 var hit = data.Keys.FirstOrDefault(k => string.Equals(k, field, StringComparison.OrdinalIgnoreCase));
 if (hit is not null)
 {
 data[hit] = "***REDACTED***";
 return true;
 }
 return false;
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

 // (3) JSON string form (stream-parse with Utf8JsonReader to avoid JsonDocument allocations)
 if (v is string s)
 {
 foreach (var f in ParseViolationFieldsFromJsonString(s))
 yield return f;
 }
 }

 private static IEnumerable<string> ParseViolationFieldsFromJsonString(string json)
 {
 // Expecting an array of objects: [{"Code":"ForbiddenField","Field":"ssn"}, ...]
 try
 {
 var bytes = Encoding.UTF8.GetBytes(json);
 var reader = new Utf8JsonReader(bytes, isFinalBlock: true, state: default);

 // Walk until start array
 if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
 return Array.Empty<string>();

 var results = new List<string>();

 while (reader.Read())
 {
 if (reader.TokenType == JsonTokenType.StartObject)
 {
 string? code = null;
 string? field = null;

 while (reader.Read())
 {
 if (reader.TokenType == JsonTokenType.PropertyName)
 {
 var propName = reader.GetString();
 if (!reader.Read()) break; // invalid

 if (string.Equals(propName, "Code", StringComparison.OrdinalIgnoreCase) && reader.TokenType == JsonTokenType.String)
 {
 code = reader.GetString();
 }
 else if (string.Equals(propName, "Field", StringComparison.OrdinalIgnoreCase) && reader.TokenType == JsonTokenType.String)
 {
 field = reader.GetString();
 }
 else
 {
 // Skip nested values we don't care about
 if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
 {
 using var doc = JsonDocument.ParseValue(ref reader);
 }
 }
 }
 else if (reader.TokenType == JsonTokenType.EndObject)
 {
 break;
 }
 }

 if (IsForbiddenCode(code))
 {
 if (field is not null) results.Add(field);
 }
 }
 else if (reader.TokenType == JsonTokenType.EndArray)
 {
 break;
 }
 }

 return results;
 }
 catch
 {
 return Array.Empty<string>(); // ignore malformed JSON
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

 // If watcher signaled or cache empty, reload under lock. This avoids a File.GetLastWriteTimeUtc on every call.
 if (_policyRedactFields.Count ==0 || Interlocked.Exchange(ref _policyStale,0) ==1)
 {
 lock (_policyLock)
 {
 if (_policyRedactFields.Count ==0 || _lastLoadedUtc < File.GetLastWriteTimeUtc(_configPath))
 {
 _policyRedactFields = ParsePolicyRedactFields(_configPath, _profileName);
 _lastLoadedUtc = File.GetLastWriteTimeUtc(_configPath);
 }
 }
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
 try
 {
 // Parse from stream to avoid allocating a temporary large string
 using var fs = File.OpenRead(path);
 using var doc = JsonDocument.Parse(fs);
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
 catch
 {
 // Malformed JSON or IO error: return empty set rather than throw to keep runtime resilient
 return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 }
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
 var dict = RentDictionary();
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

 private static Dictionary<string, object> AsDictionary(IDictionary<string, object> source, out bool rented)
 {
 if (source is Dictionary<string, object> d)
 {
 rented = false;
 return d;
 }

 var dict = RentDictionary();
 foreach (var kv in source)
 dict[kv.Key] = kv.Value;
 rented = true;
 return dict;
 }

 private static void CopyInto(Dictionary<string, object> from, IDictionary<string, object> to)
 {
 foreach (var kv in from)
 to[kv.Key] = kv.Value;
 }

 private static Dictionary<string, object> RentDictionary()
 {
 if (_dictPool.TryTake(out var d))
 {
 d.Clear();
 return d;
 }
 return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
 }

 private static void ReturnDictionaryToPool(Dictionary<string, object> d)
 {
 d.Clear();
 _dictPool.Add(d);
 }

 private static HashSet<string> RentHashSet()
 {
 if (_hashSetPool.TryTake(out var s))
 {
 s.Clear();
 return s;
 }
 return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 }

 private static void ReturnHashSet(HashSet<string> s)
 {
 s.Clear();
 _hashSetPool.Add(s);
 }
}
