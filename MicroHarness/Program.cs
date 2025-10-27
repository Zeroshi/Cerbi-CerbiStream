using System.Diagnostics;
using Microsoft.Extensions.Logging;
using CerbiStream.GovernanceRuntime.Governance;

// Minimal harness that exercises the GovernanceLogger directly in a tight loop.

var factory = LoggerFactory.Create(b =>
{
 b.SetMinimumLevel(LogLevel.Trace);
 b.AddProvider(new NoopProvider());
});

var configPath = Path.Combine(AppContext.BaseDirectory, "cerbi_governance.json");
if (!File.Exists(configPath))
{
 var minimalJson = "{\n \"Version\": \"1.0\",\n \"LoggingProfiles\": {\n \"default\": {\n \"DisallowedFields\": [],\n \"FieldSeverities\": {}\n }\n }\n}";
 File.WriteAllText(configPath, minimalJson);
}

var adapter = new GovernanceRuntimeAdapter(profileName: "default", configPath: configPath);
var provider = new GovernanceLoggerProvider(factory, adapter);
var logger = provider.CreateLogger("MicroHarness");

// Create payload
var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
{
 ["Message"] = "User checkout",
 ["userId"] =12345,
 ["email"] = "test@example.com",
 ["cartValue"] =199.95,
 ["items"] =5,
 ["ipAddress"] = "192.168.1.10",
 ["sessionId"] = Guid.NewGuid().ToString("n"),
 ["region"] = "us-east",
 ["timestamp"] = DateTime.UtcNow,
 ["note"] = "ok"
};

Console.WriteLine("Starting tight-loop micro-harness. Hit Ctrl+C to stop.");

var sw = Stopwatch.StartNew();
long count =0;

while (true)
{
 logger.Log(LogLevel.Information, default, payload, null, (s, e) => string.Empty);
 count++;
 if (count %100000 ==0)
 {
 Console.WriteLine($"Messages: {count:N0} ({sw.Elapsed.TotalSeconds:F1}s)");
 }
}


// NoopProvider reproduced locally for the harness
internal sealed class NoopProvider : ILoggerProvider
{
 private sealed class NoopLogger : ILogger
 {
 public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
 public bool IsEnabled(LogLevel logLevel) => true;
 public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

 private sealed class NullScope : IDisposable
 {
 public static readonly NullScope Instance = new();
 public void Dispose() { }
 }
 }

 public ILogger CreateLogger(string categoryName) => new NoopLogger();
 public void Dispose() { }
}
