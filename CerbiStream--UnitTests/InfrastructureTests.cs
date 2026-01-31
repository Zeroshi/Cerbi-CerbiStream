using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using CerbiStream.Configuration;
using CerbiStream.Logging.Configuration;

public class InfrastructureTests
{
 [Fact]
 public void AddCerbiStream_RegistersServices()
 {
 var services = new ServiceCollection();
 services.AddLogging(builder => builder.AddCerbiStream());

 var provider = services.BuildServiceProvider();

 // CerbiStreamOptions should be registered as a singleton
 var options = provider.GetService<CerbiStreamOptions>();
 Assert.NotNull(options);

 // Logger provider should be registered (CerbiStreamLoggerProvider or GovernanceLoggerProvider)
 var providers = provider.GetServices<ILoggerProvider>().ToList();
 Assert.Contains(providers, p => 
  p.GetType().Name.Contains("CerbiStreamLoggerProvider") || 
  p.GetType().Name.Contains("GovernanceLoggerProvider") ||
  p.GetType().Name.Contains("CerbiStream"));

 // HealthHostedService should be registered as an IHostedService
 var hosted = provider.GetServices<IHostedService>().ToList();
 Assert.Contains(hosted, h => h.GetType().Name.Contains("HealthHostedService") || h.GetType().Name.Contains("EncryptedFileRotationService"));
 }

 [Fact]
 public void TelemetryContext_Snapshot_IncludesValues()
 {
 // Set and verify TelemetryContext snapshot
 CerbiStream.Telemetry.TelemetryContext.ServiceName = "TestService";
 CerbiStream.Telemetry.TelemetryContext.Feature = "CheckoutFlow";

 var snap = CerbiStream.Telemetry.TelemetryContext.Snapshot();
 Assert.Equal("TestService", snap["ServiceName"].ToString());
 Assert.Equal("CheckoutFlow", snap["Feature"].ToString());

 // cleanup
 CerbiStream.Telemetry.TelemetryContext.Clear();
 }

 [Fact]
 public void RentDictionary_Returns_ClearedInstance()
 {
 // Find GovernanceRuntimeAdapter type by scanning loaded assemblies
 var adapterType = AppDomain.CurrentDomain.GetAssemblies()
 .SelectMany(a =>
 {
 try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
 })
 .FirstOrDefault(t => t.Name == "GovernanceRuntimeAdapter");

 Assert.NotNull(adapterType);

 var rentMethod = adapterType.GetMethod("RentDictionary", BindingFlags.NonPublic | BindingFlags.Static);
 var returnMethod = adapterType.GetMethod("ReturnDictionaryToPool", BindingFlags.NonPublic | BindingFlags.Static);
 Assert.NotNull(rentMethod);
 Assert.NotNull(returnMethod);

 var dict = rentMethod.Invoke(null, null) as Dictionary<string, object>;
 Assert.NotNull(dict);
 dict["x"] =1;

 // return to pool
 returnMethod.Invoke(null, new object[] { dict });

 // rent again
 var dict2 = rentMethod.Invoke(null, null) as Dictionary<string, object>;
 Assert.NotNull(dict2);
 Assert.False(dict2.ContainsKey("x"));

 // cleanup
 returnMethod.Invoke(null, new object[] { dict2 });
 }

 [Fact]
 public void ParsePolicyRedactFields_MalformedJson_ReturnsEmpty()
 {
 // Find GovernanceRuntimeAdapter type by scanning loaded assemblies
 var adapterType = AppDomain.CurrentDomain.GetAssemblies()
 .SelectMany(a =>
 {
 try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
 })
 .FirstOrDefault(t => t.Name == "GovernanceRuntimeAdapter");

 Assert.NotNull(adapterType);

 var parseMethod = adapterType.GetMethod("ParsePolicyRedactFields", BindingFlags.NonPublic | BindingFlags.Static);
 Assert.NotNull(parseMethod);

 var temp = System.IO.Path.GetTempFileName();
 try
 {
 System.IO.File.WriteAllText(temp, "this is not json");
 var result = parseMethod.Invoke(null, new object[] { temp, "default" }) as System.Collections.Generic.HashSet<string>;
 Assert.NotNull(result);
 Assert.Empty(result);
 }
 finally
 {
 System.IO.File.Delete(temp);
 }
 }
}
