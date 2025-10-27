using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using CerbiStream.Configuration;
using CerbiStream.GovernanceRuntime.Governance;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

namespace CerbiStream.Tests
{
 public class WiringIntegrationTests
 {
 [Fact(DisplayName = "AddCerbiGovernanceRuntime wraps inner factory and redacts forbidden fields")]
 public void AddCerbiGovernanceRuntime_Redacts_Forbidden()
 {
 var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
 File.WriteAllText(tmp,
 "{" +
 "\n \"Version\": \"1.0.0\"," +
 "\n \"LoggingProfiles\": {\n \"default\": {\n \"DisallowedFields\": [\"ssn\"]\n }\n }\n}");

 var sink = new TestSink();
 using var innerFactory = LoggerFactory.Create(b => b.AddProvider(sink));

 using var outerFactory = LoggerFactory.Create(b =>
 {
 b.AddCerbiGovernanceRuntime(innerFactory, profileName: "default", configPath: tmp);
 });

 var logger = outerFactory.CreateLogger("test");
 logger.LogInformation("{ssn} {userId}", "111-22-3333", "u1");

 Assert.Single(sink.Events);
 var kvs = sink.Events[0].State as IEnumerable<KeyValuePair<string, object>> ?? Array.Empty<KeyValuePair<string, object>>();
 var ssn = kvs.First(k => k.Key == "ssn").Value;
 Assert.Equal("***REDACTED***", ssn);
 }

 [Fact(DisplayName = "AddCerbiStream registers providers, encryption, and rotation service when enabled")]
 public void AddCerbiStream_Registers_All_When_Configured()
 {
 var tempDir = Path.Combine(Path.GetTempPath(), "cerbi_wiring_" + Guid.NewGuid().ToString("n"));
 Directory.CreateDirectory(tempDir);
 var fallback = Path.Combine(tempDir, "fallback.json");
 var primary = Path.Combine(tempDir, "primary.json");

 var services = new ServiceCollection();
 services.AddLogging(builder => builder.AddCerbiStream(opts =>
 {
 opts.WithFileFallback(fallback, primary);
 opts.WithAesEncryption();
 var key = Encoding.UTF8.GetBytes("1234567890123456");
 var iv = Encoding.UTF8.GetBytes("1234567890123456");
 opts.WithEncryptionKey(key, iv);
 }));

 using var sp = services.BuildServiceProvider();

 // Providers are wired
 var providers = sp.GetServices<ILoggerProvider>().ToList();
 Assert.Contains(providers, p => p.GetType().Name.Contains("CerbiStreamLoggerProvider"));
 Assert.Contains(providers, p => p.GetType().Name.Contains("FileFallbackProvider"));

 // Hosted services include rotation and health
 var hosted = sp.GetServices<IHostedService>().ToList();
 Assert.Contains(hosted, h => h.GetType().Name.Contains("EncryptedFileRotationService"));
 Assert.Contains(hosted, h => h.GetType().Name.Contains("HealthHostedService"));

 // Encryption is resolved and AES enabled
 var enc = sp.GetRequiredService<CerbiClientLogging.Interfaces.IEncryption>();
 Assert.True(enc.IsEnabled);
 Assert.Equal(EncryptionType.AES, enc.EncryptionMethod);

 // LoggerFactory can create a logger and call
 var factory = sp.GetRequiredService<ILoggerFactory>();
 var logger = factory.CreateLogger("wire");
 logger.LogInformation("wired");

 // cleanup
 try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); } catch { }
 }
 }
}
