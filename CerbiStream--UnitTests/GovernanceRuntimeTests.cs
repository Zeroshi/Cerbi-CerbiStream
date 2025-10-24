using CerbiStream.GovernanceRuntime.Governance;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CerbiStream.Tests;

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
}
