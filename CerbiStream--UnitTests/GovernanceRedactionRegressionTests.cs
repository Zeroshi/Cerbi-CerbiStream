extern alias GovernanceCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CerbiStream.GovernanceRuntime.Governance;
using Microsoft.Extensions.Logging;
using Xunit;
using Violation = GovernanceCore::Cerbi.Governance.GovernanceViolation;

namespace CerbiStream.Tests;

public class GovernanceRedactionRegressionTests
{
    private const string Email = "proof@example.test";

    [Fact]
    public void RepeatedSensitiveFieldEndingWithRedactionMarkerCannotBypassFormatting()
    {
        using var f = new Fixture();
        f.Logger.LogInformation("{email} then {email}; {step} {step}", Email, "***REDACTED***", 1, 2);
        Assert.Equal("***REDACTED*** then ***REDACTED***; 1 2", f.Sink.Message);
        Assert.DoesNotContain(Email, f.Sink.Message);
    }

    [Fact]
    public void CustomFormatterCannotReintroduceRedactedValue()
    {
        using var f = new Fixture();
        var state = new Dictionary<string, object> { ["email"] = Email, ["traceId"] = "trace-001", ["durationMs"] = 42 };
        var exception = new InvalidOperationException("synthetic diagnostic");
        f.Logger.Log(LogLevel.Warning, new EventId(73, "synthetic"), state, exception,
            (_, _) => throw new InvalidOperationException("Original formatter must not run after redaction"));
        Assert.Equal("***REDACTED***", f.Sink.State["email"]);
        Assert.Equal("trace-001", f.Sink.State["traceId"]);
        Assert.Equal(42, f.Sink.State["durationMs"]);
        Assert.DoesNotContain(Email, f.Sink.Message);
        Assert.DoesNotContain(Email, JsonSerializer.Serialize(f.Sink.State));
        Assert.Equal(LogLevel.Warning, f.Sink.Level);
        Assert.Equal(73, f.Sink.Event.Id);
        Assert.Equal("synthetic", f.Sink.Event.Name);
        Assert.Equal("proof-category", f.Sink.Category);
        Assert.Same(exception, f.Sink.Exception);
    }

    [Fact]
    public void StructuredTemplatePreservesFormattingAndRepeatedArguments()
    {
        using var f = new Fixture();
        f.Logger.LogInformation("{{literal}} {email} {step} {step} {amount,8:F2} {nullable} {items}",
            Email, 1, 2, 12.5, null, new[] { 3, 4 });
        Assert.Equal("{literal} ***REDACTED*** 1 2    12.50 (null) 3, 4", f.Sink.Message);
        Assert.DoesNotContain(Email, JsonSerializer.Serialize(f.Sink.State));
    }

    [Fact]
    public void EveryRepeatedSensitivePlaceholderIsRedacted()
    {
        using var f = new Fixture();
        f.Logger.LogInformation("{email} then {email}", Email, "second@example.test");
        Assert.Equal("***REDACTED*** then ***REDACTED***", f.Sink.Message);
    }

    [Fact]
    public void MalformedOrMissingTemplateArgumentsFallBackToSanitizedJson()
    {
        using var f = new Fixture();
        foreach (var template in new[] { "bad {email", "{missing}", "{email,not-an-alignment}" })
        {
            var state = new Dictionary<string, object> { ["email"] = Email, ["{OriginalFormat}"] = template };
            f.Logger.Log(LogLevel.Information, default, state, null, (_, _) => Email);
            Assert.DoesNotContain(Email, f.Sink.Message);
            using var json = JsonDocument.Parse(f.Sink.Message);
            Assert.Equal("***REDACTED***", json.RootElement.GetProperty("email").GetString());
        }
    }

    [Fact]
    public void CleanRecordHasZeroViolationsAndPreservesOriginalFormatter()
    {
        using var f = new Fixture();
        var state = new Dictionary<string, object> { ["traceId"] = "trace-001", ["durationMs"] = 42 };
        var exception = new InvalidOperationException("synthetic diagnostic");
        f.Logger.Log(LogLevel.Warning, new EventId(74), state, exception, (s, e) => $"{s["traceId"]}: {e!.Message}");
        Assert.Equal("trace-001: synthetic diagnostic", f.Sink.Message);
        Assert.Equal("trace-001", f.Sink.State["traceId"]);
        Assert.Equal(42, f.Sink.State["durationMs"]);
        Assert.Empty(Assert.IsType<List<Violation>>(f.Sink.State["GovernanceViolations"]));
        Assert.Same(exception, f.Sink.Exception);
    }

    [Fact]
    public void UnrelatedViolationsArePreservedAndPolicyViolationsDeduplicated()
    {
        var existing = new Violation { RuleId = "OtherRule", Field = "email", Severity = "Warn" };
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
            ["email"] = Email, ["GovernanceViolations"] = new List<Violation> { existing }
        };
        Ensure(data, new[] { "email", "EMAIL", "absent" });
        Ensure(data, new[] { "email" });
        var violations = Assert.IsType<List<Violation>>(data["GovernanceViolations"]);
        Assert.Equal(2, violations.Count);
        Assert.Contains(violations, v => v.RuleId == "OtherRule");
        Assert.Single(violations.Where(v => v.RuleId == "PolicyDisallowedField" && v.Field == "email"));
        Assert.DoesNotContain(violations, v => v.Field == "absent");
    }

    [Fact]
    public void EquivalentForbiddenViolationIsNotDuplicated()
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
            ["email"] = Email,
            ["GovernanceViolations"] = new List<Violation> {
                new Violation { RuleId = "default:email:Forbidden", Code = "Forbidden", Field = "email", Severity = "Error" }
            }
        };
        Ensure(data, new[] { "email" });
        Assert.Single(Assert.IsType<List<Violation>>(data["GovernanceViolations"]));
    }

    private static void Ensure(Dictionary<string, object> data, string[] fields)
        => typeof(GovernanceRuntimeAdapter).GetMethod("EnsurePolicyViolations", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { data, fields });

    private sealed class Fixture : IDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), "cerbi-regression-" + Guid.NewGuid() + ".json");
        private readonly GovernanceLoggerProvider provider;
        public readonly CaptureFactory Sink = new();
        public ILogger Logger { get; }
        public Fixture()
        {
            File.WriteAllText(path, """{"EnforcementMode":"Strict","LoggingProfiles":{"default":{"name":"default","DisallowedFields":["email"],"AllowRelax":false}}}""");
            provider = new GovernanceLoggerProvider(Sink, new GovernanceRuntimeAdapter("default", path));
            Logger = provider.CreateLogger("proof-category");
        }
        public void Dispose() { provider.Dispose(); File.Delete(path); }
    }

    private sealed class CaptureFactory : ILoggerFactory, ILogger
    {
        public Dictionary<string, object> State = new();
        public string Message = "";
        public string Category = "";
        public LogLevel Level;
        public EventId Event;
        public Exception? Exception;
        public ILogger CreateLogger(string category) { Category = category; return this; }
        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            State = ((IEnumerable<KeyValuePair<string, object>>)state!).ToDictionary(k => k.Key, v => v.Value);
            Message = formatter(state, exception);
            Level = level; Event = id; Exception = exception;
        }
    }
}
