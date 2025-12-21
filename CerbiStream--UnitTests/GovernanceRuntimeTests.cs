using CerbiStream.GovernanceRuntime.Governance;
using CerbiStream.Observability;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;
using Cerbi.Governance;
using Xunit;

namespace CerbiStream.Tests;

[Collection("MetricsIsolation")]
public class GovernanceRuntimeTests
{
    [Fact(DisplayName = "Governance: Forbidden field is redacted and violation tagged")]
    public void Forbidden_Field_Is_Redacted_And_Violation_Tagged()
    {
        // inline config file with1 profile
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(tmp,
@"{
  ""Version"": ""1.0.0"",
  ""LoggingProfiles"": {
    ""default"": {
      ""DisallowedFields"": [""ssn""]
    }
  }
}");

        var sink = new TestSink();
        var inner = LoggerFactory.Create(b => b.AddProvider(sink));
        var adapter = new GovernanceRuntimeAdapter("default", tmp);
        var provider = new GovernanceLoggerProvider(inner, adapter);
        var logger = LoggerFactory.Create(b => b.AddProvider(provider)).CreateLogger("test");

        logger.LogInformation("{ssn} {userId}", "111-22-3333", "u1");

        Assert.Single(sink.Events);
        var kvs = sink.Events[0].State as IEnumerable<KeyValuePair<string, object>> ?? Array.Empty<KeyValuePair<string, object>>();
        var ssn = kvs.First(k => k.Key == "ssn").Value;
        Assert.Equal("***REDACTED***", ssn);
    }

    [Fact(DisplayName = "Governance: Forbidden field is retained with violation metadata")]
    public void Forbidden_Field_Is_Retained_With_Violation_Metadata()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(tmp,
@"{
  ""Version"": ""1.0.0"",
  ""LoggingProfiles"": {
    ""default"": {
      ""DisallowedFields"": [""ssn""]
    }
  }
}");

        try
        {
            var sink = new TestSink();
            var inner = LoggerFactory.Create(b => b.AddProvider(sink));
            var adapter = new GovernanceRuntimeAdapter("default", tmp);
            var provider = new GovernanceLoggerProvider(inner, adapter);
            var logger = LoggerFactory.Create(b => b.AddProvider(provider)).CreateLogger("test");

            logger.LogInformation("{ssn} {userId}", "111-22-3333", "u1");

            var kvs = sink.Events[0].State as IEnumerable<KeyValuePair<string, object>> ?? Array.Empty<KeyValuePair<string, object>>();
            var dict = kvs.ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);

            Assert.True(dict.ContainsKey("ssn"));
            Assert.Equal("***REDACTED***", dict["ssn"]);
            Assert.True(dict.ContainsKey("GovernanceViolations"));

            var violations = Assert.IsType<List<GovernanceViolation>>(dict["GovernanceViolations"]);
            var violation = Assert.Single(violations);
            Assert.Equal("ssn", violation.Field);
            Assert.False(string.IsNullOrWhiteSpace(violation.RuleId));
            Assert.False(string.IsNullOrWhiteSpace(violation.Severity));
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact(DisplayName = "Governance: Relax bypass produces zero-impact scoring")]
    public void Relax_Bypasses_Enforcement_With_Zero_Score_Impact()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(tmp,
@"{ ""LoggingProfiles"": { ""default"": { ""DisallowedFields"": [""secret""] } } }");

        try
        {
            Metrics.Reset();

            var sink = new TestSink();
            var inner = LoggerFactory.Create(b => b.AddProvider(sink));
            var adapter = new GovernanceRuntimeAdapter("default", tmp);
            var provider = new GovernanceLoggerProvider(inner, adapter);
            var logger = LoggerFactory.Create(b => b.AddProvider(provider)).CreateLogger("test");
            var relaxed = logger.RelaxGovernance();

            var state = new List<KeyValuePair<string, object?>>
            {
                new("secret", "value"),
                new("userId", "u1")
            };

            relaxed.Log(LogLevel.Information, new EventId(0), state, null, (_, __) => "relaxed");

            var kvs = sink.Events[0].State as IEnumerable<KeyValuePair<string, object>> ?? Array.Empty<KeyValuePair<string, object>>();
            var dict = kvs.ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);

            Assert.Equal("value", dict["secret"]);
            Assert.True(dict.TryGetValue("GovernanceRelaxed", out var relaxedFlag) && Equals(relaxedFlag, true));

            if (dict.TryGetValue("ScoreImpact", out var scoreImpact))
            {
                Assert.Equal(0, Convert.ToInt32(scoreImpact));
            }
            else
            {
                Assert.Equal(0, Metrics.Violations);
            }
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact(DisplayName = "Governance: Relax tag bypasses redaction")]
    public void Relax_Tag_Bypasses_Redaction()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(tmp,
@"{ ""LoggingProfiles"": { ""default"": { ""DisallowedFields"": [""secret""] } } }");

        var sink = new TestSink();
        var inner = LoggerFactory.Create(b => b.AddProvider(sink));
        var adapter = new GovernanceRuntimeAdapter("default", tmp);
        var provider = new GovernanceLoggerProvider(inner, adapter);
        var logger = LoggerFactory.Create(b => b.AddProvider(provider)).CreateLogger("test");

        // Provide relaxed flag as part of the structured state
        logger.LogInformation("{GovernanceRelaxed} {secret}", true, "value");

        var kvs = sink.Events[0].State as IEnumerable<KeyValuePair<string, object>> ?? Array.Empty<KeyValuePair<string, object>>();
        Assert.Equal("value", kvs.First(k => k.Key == "secret").Value);              // not redacted
        Assert.Equal(true, kvs.First(k => k.Key == "GovernanceRelaxed").Value);      // tag preserved
    }

    [Fact(DisplayName = "Governance: Metadata contract uses Cerbi.Governance.Core models")]
    public void Governance_Metadata_Contract_Locked()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(tmp,
@"{
  ""EnforcementMode"": ""Strict"",
  ""LoggingProfiles"": {
    ""default"": {
      ""DisallowedFields"": [""ssn""],
      ""AllowRelax"": false
    }
  }
}");

        try
        {
            var adapter = new GovernanceRuntimeAdapter("default", tmp);
            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["ssn"] = "111-22-3333",
                ["userId"] = "user-1"
            };

            adapter.ValidateAndRedactInPlace(data);

            var violations = Assert.IsType<List<GovernanceViolation>>(data["GovernanceViolations"]);
            var violation = Assert.Single(violations);
            Assert.Equal("ssn", violation.Field);
            Assert.False(string.IsNullOrWhiteSpace(violation.RuleId));
            Assert.False(string.IsNullOrWhiteSpace(violation.Severity));

            Assert.True(data.TryGetValue("GovernanceProfileUsed", out var profile));
            Assert.Equal("default", profile);
            Assert.True(data.TryGetValue("GovernanceEnforced", out var enforced));
            Assert.True(Convert.ToBoolean(enforced));
            Assert.True(data.TryGetValue("GovernanceMode", out var mode));
            Assert.False(string.IsNullOrWhiteSpace(mode?.ToString()));

            var json = JsonSerializer.Serialize(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("GovernanceViolations", out var violationsJson));
            Assert.Equal(JsonValueKind.Array, violationsJson.ValueKind);
            var first = violationsJson[0];
            Assert.True(first.TryGetProperty("RuleId", out _));
            Assert.True(first.TryGetProperty("Field", out _));
            Assert.True(first.TryGetProperty("Severity", out _));
            Assert.True(root.TryGetProperty("GovernanceProfileUsed", out var profileJson));
            Assert.Equal(JsonValueKind.String, profileJson.ValueKind);
            Assert.True(root.TryGetProperty("GovernanceEnforced", out var enforcedJson));
            Assert.Equal(JsonValueKind.True, enforcedJson.ValueKind);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
