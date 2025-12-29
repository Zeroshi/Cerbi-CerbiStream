extern alias GovernanceCore;

using Cerbi.Governance;
using CerbiStream.GovernanceRuntime.Governance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using GovernanceViolation = GovernanceCore::Cerbi.Governance.GovernanceViolation;

namespace CerbiStream.Tests
{
    [Collection("MetricsIsolation")]
    public class GovernanceRuntimeAdapterTests
    {
        [Fact(DisplayName = "ParseViolationFieldsFromJsonString - Valid JSON returns forbidden fields")]
        public void ParseViolationFieldsFromJsonString_ValidJson_ReturnsFields()
        {
            var method = typeof(GovernanceRuntimeAdapter).GetMethod("ParseViolationFieldsFromJsonString", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            string json = "[{\"Code\":\"ForbiddenField\",\"Field\":\"ssn\"},{\"Code\":\"Info\",\"Field\":\"notused\"}]";
            var result = method.Invoke(null, new object[] { json }) as System.Collections.IEnumerable;
            Assert.NotNull(result);

            var list = new List<string>();
            foreach (var r in result)
                list.Add(r as string);

            Assert.Contains("ssn", list);
            Assert.DoesNotContain("notused", list);
        }

        [Fact(DisplayName = "Governance: GovernanceViolations string tags map to ViolationDto losslessly")]
        public void Governance_Violations_String_Tags_Map_To_ViolationDto()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "{\"Version\":\"1.0\",\"LoggingProfiles\":{\"default\":{}}}");
                var adapter = new GovernanceRuntimeAdapter("default", temp);

                var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["GovernanceViolations"] = "[{\"RuleId\":\"ForbiddenField\",\"Field\":\"ssn\",\"Severity\":\"Error\",\"Message\":\"Sensitive\"}]"
                };

                adapter.ValidateAndRedactInPlace(payload);

                var violations = Assert.IsType<List<GovernanceViolation>>(payload["GovernanceViolations"]);
                var violation = Assert.Single(violations);
                Assert.Equal("ForbiddenField", violation.RuleId);
                Assert.Equal("ssn", violation.Field);
                Assert.Equal("Error", violation.Severity);
                Assert.Equal("Sensitive", violation.Message);

                var dtoAssembly = typeof(GovernanceViolation).Assembly;
                var dtoType = dtoAssembly.GetTypes().FirstOrDefault(t => string.Equals(t.Name, "ViolationDto", StringComparison.OrdinalIgnoreCase))
                    ?? dtoAssembly.GetTypes().FirstOrDefault(t => string.Equals(t.Name, nameof(GovernanceViolation), StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(dtoType);

                var dtoListType = typeof(List<>).MakeGenericType(dtoType!);
                var serialized = JsonSerializer.Serialize(violations);
                var deserialized = JsonSerializer.Deserialize(serialized, dtoListType) as System.Collections.IEnumerable;

                Assert.NotNull(deserialized);
                var enumerator = deserialized!.GetEnumerator();
                Assert.True(enumerator.MoveNext());

                var dto = enumerator.Current!;
                string? Prop(string name) => dtoType!.GetProperty(name)?.GetValue(dto)?.ToString();

                Assert.Equal(violation.RuleId, Prop("RuleId") ?? Prop("Code"));
                Assert.Equal(violation.Field, Prop("Field"));
                Assert.Equal(violation.Severity, Prop("Severity"));
                Assert.Equal(violation.Message, Prop("Message"));
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Fact(DisplayName = "ParseViolationFieldsFromJsonString - Malformed JSON returns empty")]
        public void ParseViolationFieldsFromJsonString_MalformedJson_ReturnsEmpty()
        {
            var method = typeof(GovernanceRuntimeAdapter).GetMethod("ParseViolationFieldsFromJsonString", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            string json = "this is not json";
            var result = method.Invoke(null, new object[] { json }) as System.Collections.IEnumerable;
            Assert.NotNull(result);

            var enumerator = result.GetEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact(DisplayName = "ValidateAndRedactInPlace - Redacts fields from policy file")]
        public void ValidateAndRedactInPlace_RedactsFromPolicy()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "{\"Version\":\"1.0\",\"LoggingProfiles\":{\"default\":{\"DisallowedFields\":[\"secret\"],\"FieldSeverities\":{}}}}");

                var adapter = new GovernanceRuntimeAdapter("default", temp);

                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["secret"] = "topsecret",
                    ["other"] = "value"
                };

                adapter.ValidateAndRedactInPlace(data);

                Assert.Equal("***REDACTED***", data["secret"]);
                Assert.Equal("value", data["other"]);
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Fact(DisplayName = "ValidateAndRedactInPlace - Concurrent calls redact consistently")]
        public void ConcurrentValidate_DoesNotThrow_And_RedactsConsistently()
        {
            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "{\"Version\":\"1.0\",\"LoggingProfiles\":{\"default\":{\"DisallowedFields\":[\"secret\"],\"FieldSeverities\":{}}}}");
                var adapter = new GovernanceRuntimeAdapter("default", temp);

                var tasks = new List<Task>();
                for (int i = 0; i < 20; i++)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["secret"] = "topsecret",
                            ["n"] = i
                        };

                        adapter.ValidateAndRedactInPlace(data);
                        Assert.Equal("***REDACTED***", data["secret"]);
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Fact(DisplayName = "PolicyReload - File change updates redaction policy")]
        public void PolicyReload_OnFileChange_UpdatesRedaction()
        {
            var temp = Path.Combine(Path.GetTempPath(), "cerbi_policy_test.json");
            try
            {
                // initial policy with no disallowed fields
                File.WriteAllText(temp, "{\"Version\":\"1.0\",\"LoggingProfiles\":{\"default\":{\"DisallowedFields\":[],\"FieldSeverities\":{}}}}");
                var adapter = new GovernanceRuntimeAdapter("default", temp);

                var data1 = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["secret"] = "topsecret"
                };
                adapter.ValidateAndRedactInPlace(data1);
                Assert.Equal("topsecret", data1["secret"]);

                // update policy to include 'secret'
                File.WriteAllText(temp, "{\"Version\":\"1.0\",\"LoggingProfiles\":{\"default\":{\"DisallowedFields\":[\"secret\"],\"FieldSeverities\":{}}}}");
                // Touch file time
                File.SetLastWriteTimeUtc(temp, DateTime.UtcNow.AddSeconds(1));

                var data2 = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["secret"] = "topsecret"
                };

                // Allow some time for watcher to observe change (if watcher available); adapter also checks timestamp on call
                adapter.ValidateAndRedactInPlace(data2);
                Assert.Equal("***REDACTED***", data2["secret"]);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        [Fact(DisplayName = "ValidateAndRedactInPlace(JsonElement) returns pooled dictionaries")]
        public void ValidateAndRedactInPlace_JsonElement_ReturnsDictionaryToPool()
        {
            var adapterType = typeof(GovernanceRuntimeAdapter);
            var poolField = adapterType.GetField("_dictPool", BindingFlags.NonPublic | BindingFlags.Static);
            var returnMethod = adapterType.GetMethod("ReturnDictionaryToPool", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(poolField);
            Assert.NotNull(returnMethod);

            var pool = (ConcurrentBag<Dictionary<string, object>>)poolField!.GetValue(null)!;

            // Clear pool for deterministic counts
            while (pool.TryTake(out _)) { }

            // Seed the pool with a known dictionary
            returnMethod!.Invoke(null, new object[] { new Dictionary<string, object>() });
            var initialCount = pool.Count;

            var temp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temp, "{\"Version\":\"1.0\",\"LoggingProfiles\":{\"default\":{\"DisallowedFields\":[],\"FieldSeverities\":{}}}}");
                var adapter = new GovernanceRuntimeAdapter("default", temp);

                var json = JsonDocument.Parse("{\"secret\":\"v\"}").RootElement;
                adapter.ValidateAndRedactInPlace(json);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }

            var finalCount = pool.Count;
            Assert.Equal(initialCount, finalCount);
        }
    }
}
