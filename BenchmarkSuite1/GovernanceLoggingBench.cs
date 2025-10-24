using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using CerbiStream.GovernanceRuntime.Governance;
using Microsoft.VSDiagnostics;

namespace CerbiStream.Benchmarks
{
    [MemoryDiagnoser]
    [CPUUsageDiagnoser]
    public class GovernanceLoggingBench
    {
        private ILogger _baselineLogger = default!;
        private ILogger _governanceLogger = default!;
        private Dictionary<string, object> _payload = default!;
        private Dictionary<string, object> _relaxedPayload = default!;

        private sealed class NoopProvider : ILoggerProvider
        {
            private sealed class NoopLogger : ILogger
            {
                public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    // no-op sink
                }

                private sealed class NullScope : IDisposable
                {
                    public static readonly NullScope Instance = new();
                    public void Dispose() { }
                }
            }

            public ILogger CreateLogger(string categoryName) => new NoopLogger();
            public void Dispose() { }
        }

        [GlobalSetup]
        public void Setup()
        {
            // Ensure a minimal governance file exists to avoid runtime exceptions
            var configPath = Path.Combine(AppContext.BaseDirectory, "cerbi_governance.json");
            if (!File.Exists(configPath))
            {
                var minimalJson = "{\n \"Version\": \"1.0\",\n \"LoggingProfiles\": {\n \"default\": {\n \"DisallowedFields\": [],\n \"FieldSeverities\": {}\n }\n }\n}";
                File.WriteAllText(configPath, minimalJson);
            }

            var innerFactory = LoggerFactory.Create(b =>
            {
                b.SetMinimumLevel(LogLevel.Trace);
                b.AddProvider(new NoopProvider());
            });

            _baselineLogger = innerFactory.CreateLogger("Bench");

            var adapter = new GovernanceRuntimeAdapter(profileName: "default", configPath: configPath);
            var provider = new GovernanceLoggerProvider(innerFactory, adapter);
            _governanceLogger = provider.CreateLogger("Bench");

            _payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Message"] = "User checkout",
                ["userId"] = 12345,
                ["email"] = "test@example.com",
                ["cartValue"] = 199.95,
                ["items"] = 5,
                ["ipAddress"] = "192.168.1.10",
                ["sessionId"] = Guid.NewGuid().ToString("n"),
                ["region"] = "us-east",
                ["timestamp"] = DateTime.UtcNow,
                ["note"] = "ok"
            };
            _relaxedPayload = new Dictionary<string, object>(_payload, StringComparer.OrdinalIgnoreCase)
            {
                ["GovernanceRelaxed"] = true
            };
        }

        [Benchmark(Baseline = true)]
        public void Baseline_InnerLogger()
        {
            _baselineLogger.Log(LogLevel.Information, eventId: default, _payload, null, (s, e) => string.Empty);
        }

        [Benchmark]
        public void WithGovernance()
        {
            _governanceLogger.Log(LogLevel.Information, eventId: default, _payload, null, (s, e) => string.Empty);
        }

        [Benchmark]
        public void WithGovernance_Relaxed()
        {
            _governanceLogger.Log(LogLevel.Information, eventId: default, _relaxedPayload, null, (s, e) => string.Empty);
        }
    }
}