using CerbiStream.GovernanceRuntime.Governance;
using CerbiStream.Logging.Configuration;
using CerbiStream.Scoring;
using CerbiShield.Contracts.Scoring;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace CerbiStream.Tests;

public sealed class GovernanceSummaryAccumulatorTests
{
    [Fact]
    public void Matches_Canonical_Vector()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(ContractFixture("governance-summary-batch.schema.json")));
        Assert.Equal("https://cerbi.io/schemas/runtime/v1/governance-summary-batch.schema.json", schema.RootElement.GetProperty("$id").GetString());

        using var vector = JsonDocument.Parse(File.ReadAllText(ContractFixture("governance-summary-sequence.example.json")));
        var root = vector.RootElement;
        var accumulator = new GovernanceSummaryAccumulator(OptionsFromVector(root));
        var windowStart = DateTimeOffset.Parse(root.GetProperty("windowStartUtc").GetString()!);

        foreach (var ev in root.GetProperty("events").EnumerateArray())
        {
            accumulator.Record(new GovernanceSummaryEvent(
                ev.GetProperty("outcome").GetString()!,
                ev.GetProperty("violations").EnumerateArray()
                    .Select(v => new GovernanceSummaryViolation(
                        v.GetProperty("ruleId").GetString()!,
                        v.GetProperty("action").GetString()!,
                        v.GetProperty("severity").GetString()!))
                    .ToArray()), windowStart);
        }

        var batch = accumulator.Flush();
        Assert.NotNull(batch);
        var wire = JsonSerializer.SerializeToElement(batch);
        var summary = wire.GetProperty("summaries")[0];
        Assert.True(JsonElementDeepEquals(summary.GetProperty("counters"), root.GetProperty("expectedSummary").GetProperty("counters")));
        Assert.Equal(root.GetProperty("emitterId").GetString(), summary.GetProperty("emitterId").GetString());
        Assert.Equal(root.GetProperty("streamId").GetString(), summary.GetProperty("streamId").GetString());
        Assert.Contains($"|{root.GetProperty("emitterId").GetString()}|{root.GetProperty("streamId").GetString()}|", summary.GetProperty("summaryId").GetString());
        Assert.Equal(
            RuleCounts(root.GetProperty("expectedSummary").GetProperty("ruleCounts")),
            RuleCounts(summary.GetProperty("ruleCounts")));

        var counters = summary.GetProperty("counters");
        Assert.Equal(
            counters.GetProperty("passed").GetInt64()
            + counters.GetProperty("violating").GetInt64()
            + counters.GetProperty("redacted").GetInt64()
            + counters.GetProperty("blocked").GetInt64()
            + counters.GetProperty("relaxed").GetInt64()
            + counters.GetProperty("errors").GetInt64(),
            counters.GetProperty("evaluated").GetInt64());
        var text = wire.ToString();
        Assert.DoesNotContain("prompt", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("completion", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payload", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("message", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rawEvent", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telemetry", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cumulative_Snapshots_Retry_And_Identity_Are_Stable()
    {
        var accumulator = new GovernanceSummaryAccumulator(TestOptions());
        var at = DateTimeOffset.Parse("2026-09-09T14:29:05Z");
        accumulator.Record(new GovernanceSummaryEvent("passed", Array.Empty<GovernanceSummaryViolation>()), at);
        var first = accumulator.Snapshot();
        Assert.NotNull(first);
        accumulator.MarkFlushed(first);
        Assert.Null(accumulator.Snapshot());

        accumulator.Record(new GovernanceSummaryEvent("redacted", new[]
        {
            new GovernanceSummaryViolation("pii.email", "redact", "warning"),
            new GovernanceSummaryViolation("pii.phone", "redact", "warning")
        }), DateTimeOffset.Parse("2026-09-09T14:29:35Z"));
        var second = accumulator.Snapshot();
        Assert.NotNull(second);
        Assert.Equal(Summary(first).SummaryId, Summary(second).SummaryId);
        Assert.Equal(2, Summary(second).Counters["evaluated"]);
        Assert.Equal(1, Summary(second).Counters["redacted"]);
        Assert.Equal(2, Summary(second).Counters["violations"]);

        var manual = new GovernanceSummaryAccumulator(TestOptions());
        manual.Record(new GovernanceSummaryEvent("passed", Array.Empty<GovernanceSummaryViolation>()), at);
        var firstManual = manual.Flush();
        var secondManual = manual.Flush();
        Assert.NotNull(firstManual);
        Assert.NotNull(secondManual);
        Assert.Equal(Summary(firstManual).Counters, Summary(secondManual).Counters);
        manual.MarkFlushed(secondManual);
        Assert.Null(manual.Snapshot());

        var replica = TestOptions();
        replica.EmitterId = "orders-api-pod-b";
        replica.StreamId = "orders-api-pod-b-20260909T142856Z";
        var restart = TestOptions();
        restart.StreamId = "orders-api-pod-a-20260909T142945Z";
        var a = new GovernanceSummaryAccumulator(TestOptions());
        var b = new GovernanceSummaryAccumulator(replica);
        var c = new GovernanceSummaryAccumulator(restart);
        a.Record(new GovernanceSummaryEvent("passed", Array.Empty<GovernanceSummaryViolation>()), at);
        b.Record(new GovernanceSummaryEvent("passed", Array.Empty<GovernanceSummaryViolation>()), at);
        c.Record(new GovernanceSummaryEvent("passed", Array.Empty<GovernanceSummaryViolation>()), at);
        Assert.Equal(3, new[] { Summary(a.Snapshot()!).SummaryId, Summary(b.Snapshot()!).SummaryId, Summary(c.Snapshot()!).SummaryId }.Distinct().Count());
    }

    [Fact]
    public void Unknown_Action_Violation_Maps_To_Violating_Not_Redacted()
    {
        var ev = GovernanceSummaryAccumulator.EventFromFields(new Dictionary<string, object>
        {
            ["GovernanceMode"] = "Strict",
            ["GovernanceViolations"] = new List<Dictionary<string, object>>
            {
                new() { ["RuleId"] = "ForbiddenField:secret", ["Severity"] = "Warning" }
            }
        });

        Assert.Equal("violating", ev.Outcome);
        Assert.Single(ev.Violations);
        Assert.Equal("audit", ev.Violations[0].Action);
    }

    [Fact]
    public void Explicit_Redaction_Action_Maps_To_Redacted()
    {
        var ev = GovernanceSummaryAccumulator.EventFromFields(new Dictionary<string, object>
        {
            ["GovernanceMode"] = "Strict",
            ["GovernanceDecision"] = "redacted",
            ["EnforcementAction"] = "redact",
            ["GovernanceViolations"] = new List<Dictionary<string, object>>
            {
                new() { ["RuleId"] = "ForbiddenField:secret", ["Severity"] = "Warning" }
            }
        });

        Assert.Equal("redacted", ev.Outcome);
        Assert.Single(ev.Violations);
        Assert.Equal("redacted", ev.Violations[0].Action);
    }

    [Fact]
    public async Task Http_Shipper_Retries_Failed_Post_Until_Acknowledged()
    {
        var sent = new List<string>();
        var shipper = new GovernanceSummaryHttpShipper(new GovernanceSummaryHttpOptions
        {
            Endpoint = "https://scoring.example.test/ingest/governance-summary",
            TenantId = "contoso",
            AppName = "orders-api",
            Environment = "prod",
            ServiceName = "orders-api",
            EmitterId = "orders-api-pod-a",
            StreamId = "orders-api-pod-a-20260909T142855Z",
            FlushIntervalSeconds = 0,
            Now = () => DateTimeOffset.Parse("2026-09-09T14:30:00Z"),
            SendAsync = async (request, _) =>
            {
                sent.Add(await request.Content!.ReadAsStringAsync());
                return sent.Count == 1
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : new HttpResponseMessage(HttpStatusCode.Accepted);
            }
        });
        shipper.Record(new Dictionary<string, object> { ["GovernanceDecision"] = "allowed" }, DateTimeOffset.Parse("2026-09-09T14:29:05Z"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => shipper.FlushAsync());
        await shipper.FlushAsync();
        Assert.Equal(2, sent.Count);
        Assert.Equal(CountersJson(sent[0]), CountersJson(sent[1]));
        await shipper.FlushAsync();
        Assert.Equal(2, sent.Count);
    }

    [Fact]
    public async Task Adapter_Emits_Aggregate_Summary_From_Evaluated_Record()
    {
        var temp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temp, "{\"EnforcementMode\":\"Strict\",\"LoggingProfiles\":{\"default\":{\"name\":\"default\",\"version\":\"2026.07\",\"disallowedFields\":[\"secret\"],\"fieldSeverities\":{}}}}");
            var sent = new List<string>();
            var shipper = new GovernanceSummaryHttpShipper(new GovernanceSummaryHttpOptions
            {
                Endpoint = "https://scoring.example.test/ingest/governance-summary",
                TenantId = "contoso",
                AppName = "orders-api",
                Environment = "prod",
                ServiceName = "orders-api",
                EmitterId = "orders-api-pod-a",
                StreamId = "orders-api-pod-a-20260909T142855Z",
                FlushIntervalSeconds = 0,
                Now = () => DateTimeOffset.Parse("2026-09-09T14:30:00Z"),
                SendAsync = async (request, _) =>
                {
                    sent.Add(await request.Content!.ReadAsStringAsync());
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                }
            });
            var adapter = new GovernanceRuntimeAdapter("default", temp, shipper);

            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["secret"] = "fake-secret-value"
            };
            adapter.ValidateAndRedactInPlace(data);
            await shipper.FlushAsync();

            Assert.Equal("***REDACTED***", data["secret"]);
            Assert.Single(sent);
            using var doc = JsonDocument.Parse(sent[0]);
            var counters = doc.RootElement.GetProperty("summaries")[0].GetProperty("counters");
            Assert.Equal(1, counters.GetProperty("evaluated").GetInt64());
            Assert.Equal(1, counters.GetProperty("redacted").GetInt64());
            Assert.Equal(1, counters.GetProperty("violations").GetInt64());
            Assert.DoesNotContain("fake-secret-value", sent[0], StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void Legacy_Mode_Still_Ships_Healthy_Pass_To_Detailed_Scoring()
    {
        var temp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temp, "{\"EnforcementMode\":\"Strict\",\"LoggingProfiles\":{\"default\":{\"name\":\"default\",\"version\":\"2026.07\",\"disallowedFields\":[],\"fieldSeverities\":{}}}}");
            var sink = new TestSink();
            using var inner = LoggerFactory.Create(builder => builder.AddProvider(sink));
            var adapter = new GovernanceRuntimeAdapter("default", temp);
            var scoring = new CapturingScoringService();
            var options = new CerbiStreamOptions()
                .WithTenantId("contoso")
                .WithServiceName("orders-api");
            using var provider = new GovernanceLoggerProvider(inner, adapter, options, scoring);
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
            var logger = loggerFactory.CreateLogger("orders");

            logger.Log(LogLevel.Information, default, new Dictionary<string, object> { ["message"] = "ok" }, null, (_, _) => "ok");

            Assert.Single(scoring.Events);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void Aggregate_Mode_Skips_Detailed_Scoring_For_Healthy_Pass()
    {
        var temp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temp, "{\"EnforcementMode\":\"Strict\",\"LoggingProfiles\":{\"default\":{\"name\":\"default\",\"version\":\"2026.07\",\"disallowedFields\":[],\"fieldSeverities\":{}}}}");
            var sink = new TestSink();
            using var inner = LoggerFactory.Create(builder => builder.AddProvider(sink));
            var summary = new CapturingSummarySink();
            var adapter = new GovernanceRuntimeAdapter("default", temp, summary);
            var scoring = new CapturingScoringService();
            var options = new CerbiStreamOptions()
                .WithTenantId("contoso")
                .WithServiceName("orders-api")
                .WithGovernanceSummary("https://scoring.example.test/ingest/governance-summary", "contoso", flushIntervalSeconds: 0);
            using var provider = new GovernanceLoggerProvider(inner, adapter, options, scoring);
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
            var logger = loggerFactory.CreateLogger("orders");

            logger.Log(LogLevel.Information, default, new Dictionary<string, object> { ["customerId"] = "customer-1" }, null, (_, _) => "ok");

            Assert.Empty(scoring.Events);
            Assert.Single(summary.Records);
            Assert.Equal("allowed", summary.Records[0]["GovernanceDecision"]);
            Assert.Equal("none", summary.Records[0]["EnforcementAction"]);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void Aggregate_Mode_Keeps_Detailed_Scoring_For_Redacted_Event()
    {
        var temp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temp, "{\"EnforcementMode\":\"Strict\",\"LoggingProfiles\":{\"default\":{\"name\":\"default\",\"version\":\"2026.07\",\"disallowedFields\":[\"secret\"],\"fieldSeverities\":{}}}}");
            var sink = new TestSink();
            using var inner = LoggerFactory.Create(builder => builder.AddProvider(sink));
            var summary = new CapturingSummarySink();
            var adapter = new GovernanceRuntimeAdapter("default", temp, summary);
            var scoring = new CapturingScoringService();
            var options = new CerbiStreamOptions()
                .WithTenantId("contoso")
                .WithServiceName("orders-api")
                .WithGovernanceSummary("https://scoring.example.test/ingest/governance-summary", "contoso", flushIntervalSeconds: 0);
            using var provider = new GovernanceLoggerProvider(inner, adapter, options, scoring);
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
            var logger = loggerFactory.CreateLogger("orders");

            logger.Log(LogLevel.Information, default, new Dictionary<string, object> { ["secret"] = "fake-secret-value" }, null, (_, _) => "redact");

            Assert.Single(scoring.Events);
            Assert.Single(summary.Records);
            Assert.Equal("redacted", summary.Records[0]["GovernanceDecision"]);
            Assert.Equal("redact", summary.Records[0]["EnforcementAction"]);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    private static GovernanceSummarySnapshot Summary(GovernanceSummaryBatch batch) => batch.Summaries.Single();

    private static string CountersJson(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return doc.RootElement.GetProperty("summaries")[0].GetProperty("counters").ToString();
    }

    private static GovernanceSummaryOptions OptionsFromVector(JsonElement vector)
        => new()
        {
            TenantId = vector.GetProperty("tenantId").GetString(),
            RuntimeId = "cerbi-dotnet-mel",
            RuntimeVersion = "2.0.6",
            Source = "dotnet-mel",
            AppName = vector.GetProperty("appName").GetString()!,
            Environment = vector.GetProperty("environment").GetString()!,
            ServiceName = vector.GetProperty("serviceName").GetString(),
            Runtime = "dotnet-mel",
            EmitterId = vector.GetProperty("emitterId").GetString(),
            StreamId = vector.GetProperty("streamId").GetString(),
            PeriodType = vector.GetProperty("periodType").GetString()!,
            GovernanceProfile = vector.GetProperty("governanceProfile").GetString(),
            GovernanceProfileId = vector.GetProperty("governanceProfileId").GetString(),
            GovernanceProfileVersion = vector.GetProperty("governanceProfileVersion").GetString(),
            GovernanceProfileHash = vector.GetProperty("governanceProfileHash").GetString(),
            Now = () => DateTimeOffset.Parse(vector.GetProperty("windowEndUtc").GetString()!)
        };

    private static GovernanceSummaryOptions TestOptions()
        => new()
        {
            TenantId = "contoso",
            RuntimeId = "cerbi-dotnet-mel",
            RuntimeVersion = "2.0.6",
            Source = "dotnet-mel",
            AppName = "orders-api",
            Environment = "prod",
            ServiceName = "orders-api",
            Runtime = "dotnet-mel",
            EmitterId = "orders-api-pod-a",
            StreamId = "orders-api-pod-a-20260909T142855Z",
            Now = () => DateTimeOffset.Parse("2026-09-09T14:30:00Z")
        };

    private static string ContractFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CerbiStream.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "CerbiStream--UnitTests", "Fixtures", "runtime", "v1", fileName);
    }

    private static bool JsonElementDeepEquals(JsonElement left, JsonElement right)
        => JsonSerializer.SerializeToElement(left).ToString() == JsonSerializer.SerializeToElement(right).ToString();

    private static string[] RuleCounts(JsonElement ruleCounts)
        => ruleCounts.EnumerateArray()
            .Select(rule => string.Join("|",
                rule.GetProperty("ruleId").GetString(),
                rule.GetProperty("action").GetString(),
                rule.GetProperty("severity").GetString(),
                rule.GetProperty("count").GetInt64()))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

    private sealed class CapturingScoringService : IScoringService
    {
        public List<ScoringEventDto> Events { get; } = new();

        public void Enqueue(ScoringEventDto ev) => Events.Add(ev);

        public Task FlushAndDisposeAsync() => Task.CompletedTask;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class CapturingSummarySink : IGovernanceSummarySink
    {
        public List<IDictionary<string, object>> Records { get; } = new();

        public void Record(IDictionary<string, object> fields, DateTimeOffset? at = null)
            => Records.Add(new Dictionary<string, object>(fields, StringComparer.OrdinalIgnoreCase));
    }
}
