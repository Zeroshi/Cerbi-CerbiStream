using CerbiStream.GovernanceRuntime.Governance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class GovernanceRuntimeAdapterTests
{
 [Fact]
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

 [Fact]
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

 [Fact]
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

 [Fact]
 public void ConcurrentValidate_DoesNotThrow_And_RedactsConsistently()
 {
 var temp = Path.GetTempFileName();
 try
 {
 File.WriteAllText(temp, "{\"Version\":\"1.0\",\"LoggingProfiles\":{\"default\":{\"DisallowedFields\":[\"secret\"],\"FieldSeverities\":{}}}}");
 var adapter = new GovernanceRuntimeAdapter("default", temp);

 var tasks = new List<Task>();
 for (int i =0; i <20; i++)
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

 [Fact]
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
}
