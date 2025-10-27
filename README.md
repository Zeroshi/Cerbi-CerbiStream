# CerbiStream — Governance-Enforced Structured Logging for .NET

CerbiStream helps teams produce safe, standardized, and ML-ready logs. It enforces governance policies at runtime (redaction, tagging, validation) before logs reach any sink, while integrating with existing logging frameworks such as `Microsoft.Extensions.Logging` and Serilog.

This README is ordered for new users: quick overview, why it matters, how to get started, key features, integrations, performance, security & compliance, docs and troubleshooting, and business/technical selling points.

---

## Quick summary (What is CerbiStream?)
- A runtime logging layer that validates, tags, and redacts structured logs according to a policy.
- Works as a wrapper around your existing logging pipeline (MEL, Serilog plugins available).
- Keeps logging fast (sub-microsecond overhead for governance checks in typical cases) and consistent for downstream analytics and ML.

---

## Developer-friendly additions (recent)
We added several small, low-risk features to make adoption and development easier. These are enabled by default when you use the quick registration helper below.

- `AddCerbiStream` convenience registration
 - Two overloads:
 - `AddCerbiStream(this ILoggingBuilder, Action<CerbiStreamOptions>)` — pass options with fluent API.
 - `AddCerbiStream(this ILoggingBuilder)` — default, opinionated registration.
 - Registers `CerbiStreamOptions`, the `CerbiStreamLoggerProvider`, a `RuntimeGovernanceValidator` and lightweight hosted helpers.

- `HealthHostedService`
 - A tiny hosted service that checks for the presence and accessibility of the configured governance policy file at startup and logs a warning or info. Helps catch misconfiguration early in CI/CD and on boot.
 - Automatically registered when you call `AddCerbiStream(...)`.

- Telemetry & metadata helpers
 - `TelemetryContext` snapshot facility (static) and `CerbiStreamLoggerAdapter` will merge telemetry context into log metadata when configured.
 - Lightweight enrichment of `Activity` trace identifiers (`TraceId`, `SpanId`) when `EnableTracingEnrichment` is on.

- Relaxed logging wrapper
 - `RelaxedLoggerWrapper` and `logger.RelaxGovernance()` helper allow callers to mark specific logs as `GovernanceRelaxed` (bypass validation/redaction) for intentional diagnostics or developer flows.

- Performance-friendly runtime changes
 - Temporary `Dictionary<string, object>` pooling to reduce per-log allocations when converting `IDictionary` to a concrete dictionary.
 - Pooled `HashSet<string>` for `toRedact` to avoid frequent allocations.
 - Streaming parsing of JSON-formatted `GovernanceViolations` via `Utf8JsonReader` to prevent `JsonDocument` allocations on hot paths.

- Recent micro-optimization: pooled dictionary for the governance hot path
 - The governance logger now uses a lightweight pooled `Dictionary<string, object>` when converting `IEnumerable<KeyValuePair<string, object>>` state into a mutable structure for validation/redaction. This reduces per-log allocations and GC pressure on hot paths.
 - Caveat: if any downstream sink captures or mutates the provided dictionary asynchronously after the `Log` call returns, the pooled dictionary MUST NOT be returned to the pool until the sink is finished with it. The library's current pooling implementation returns the dictionary immediately; if your sinks capture state asynchronously, either disable pooling or switch sinks to copy the state before returning. We plan to make pooling opt-in in a future release.

- Tests
 - Unit tests added for the health hosted service (`CerbiStream--UnitTests/HealthHostedServiceTests.cs`) and existing test coverage continues for options, governance and telemetry providers.

Quick usage example (recommended):

```csharp
var builder = Host.CreateDefaultBuilder(args);
var appBuilder = builder.ConfigureLogging((context, logging) =>
{
 logging.AddCerbiStream(options =>
 {
 options.WithFileFallback("logs/fallback.json");
 options.WithTelemetryEnrichment(true);
 options.WithGovernanceChecks(true);
 });
});
```

This wires CerbiStream into the standard host logging system and registers the health check hosted service automatically.

How to run the health test locally:

```
dotnet test CerbiStream--UnitTests/UnitTests.csproj -f net8.0
```

---

<<<<<<< HEAD
## Dev & observability (new)
CerbiStream aims to be developer-friendly and lightweight. The library includes small, optional observability helpers you can enable in apps to get immediate insight without pulling heavy dependencies.

- Built-in metrics
 - The runtime increments three lightweight counters: `LogsProcessed`, `Redactions`, and `Violations` in `CerbiStream.Observability.Metrics`.
 - These counters are thread-safe and can be reset in tests via `Metrics.Reset()`.
 - Optionally wire metrics to a telemetry provider by setting `CerbiStreamOptions.TelemetryProvider` — the library will emit a simple `CerbiStream.Metric` event for metric updates when a provider is present.
 - Optionally expose Prometheus-style metrics via the middleware described below.

- Micro-harness for profiling (new)
 - A small console harness `MicroHarness` is included in the solution to exercise the governance logger in a tight loop without BenchmarkDotNet. Use this when collecting profiler traces to get focused hotspots inside CerbiStream.
 - Run locally for profiling (Release build with portable PDBs):

```
dotnet build MicroHarness -c Release -p:DebugType=portable -p:DebugSymbols=true
PerfView.exe -> Collect -> Run
 Program: dotnet
 Arguments: run --project MicroHarness -c Release --no-build
 Max Collect Secs:20–30
```

- Prometheus / health endpoints (opt-in)
 - Minimal middleware exposes two endpoints for development and lightweight monitoring:
 - `/cerbistream/metrics` — Prometheus-style plaintext metrics for the three counters.
 - `/cerbistream/health` — basic JSON readiness response.
 - To enable in ASP.NET Core:

```csharp
// register logging and health helpers
builder.Logging.AddCerbiStream(options => { /* ... */ });
builder.Logging.AddCerbiStreamHealthChecks();

var app = builder.Build();
// add middleware to pipeline
app.UseCerbiStreamMetrics();
```

- Telemetry integration (opt-in, minimal)
 - The library supports pluggable telemetry providers (AppInsights, OpenTelemetry, Datadog, etc.).
 - If you provide a telemetry provider via `CerbiStreamOptions.WithTelemetryProvider(...)`, CerbiStream will forward lightweight metric events.
 - This wiring is intentionally minimal and designed to be useful during development; for production-grade telemetry aggregation use your preferred telemetry pipeline or OpenTelemetry exporters.

- Keep it lightweight
 - Everything above is opt-in. If you don't call the health/metrics helper nobody is added to your request pipeline.
 - The core library has no runtime dependency on ASP.NET Core; middleware and healthchecks use small, optional extension registration and only add a couple of types.

---

=======
>>>>>>> parent of d87f8a2 (readme update)
## The problem (why this exists)
Modern apps emit high volumes of structured logs across many services and destinations. Common challenges:
- PII and secrets accidentally logged and stored in multiple systems.
- Inconsistent field names and schemas break analytics & ML pipelines.
- Compliance audits require consistent redaction and proof of enforcement.
- Storing unstandardized logs increases indexing and storage costs.

---

## The solution (what CerbiStream does)
CerbiStream enforces governance before logs are written to any sink:
- Validates logs against a `cerbi_governance.json` policy per profile.
- Tags logs with governance metadata (violations, profile version).
- Redacts disallowed/forbidden fields in-place.
- Integrates seamlessly with existing sinks and logging libraries.

---

## Quick start (5-minute setup)
1. Add the package or project reference to `LoggingStandards/CerbiStream.csproj` (or install the NuGet package).
2. Create a simple policy file `cerbi_governance.json` in your application folder or set `CERBI_GOVERNANCE_PATH`.

Example policy snippet:
```
{
 "Version": "1.0.0",
 "LoggingProfiles": {
 "default": {
 "DisallowedFields": ["ssn"],
 "FieldSeverities": { "creditCard": "Forbidden" }
 }
 }
}
```

3. Wire CerbiStream into your logging pipeline (example in `Program.cs`):
```csharp
var inner = LoggerFactory.Create(b => b.AddConsole());
builder.Logging.AddCerbiGovernanceRuntime(inner, "default", configPath: "./cerbi_governance.json");
```
OR use the convenience helper:
```csharp
builder.Logging.AddCerbiStream(options =>
{
 options.WithFileFallback("logs/fallback.json");
});
```
4. Run. Logs that include `ssn` or `creditCard` fields will be redacted as `***REDACTED***` and governance tags will be present.

For more: see `docs/INSTALLATION.md` and `docs/README-PRODUCTION.md`.

---

## Key features (at a glance)
- Runtime governance enforcement (validate, tag, redact)
- Profile-based policies (`LoggingProfiles`) and env override via `CERBI_GOVERNANCE_PATH`
- In-place, case-insensitive redaction for structured logs
- Relaxed mode (`GovernanceRelaxed`) to bypass enforcement when intentional
- Low-latency: typical governance overhead ≈0.65 µs per log in our benchmarks
- Memory-efficient: pooled dictionaries & hashsets, streaming JSON parsing
- Integrations with AppInsights, OpenTelemetry, Datadog, AWS CloudWatch, GCP Stackdriver
- Queue + storage sinks: Azure, AWS SQS/Kinesis/S3, Google Pub/Sub/Storage, RabbitMQ, Kafka
- File fallback with rotation and optional encryption (AES/Base64)
- Configurable retry policies (Polly) and telemetry enrichment
- Unit tests and benchmark suite included (`CerbiStream--UnitTests`, `BenchmarkSuite1`)

---

## Integrations and extensibility
- Works as a middleware/provider for `Microsoft.Extensions.Logging` via `AddCerbiGovernanceRuntime`.
- Plug-in model and adapters available for Serilog so you can keep existing sinks and structured logging code.
- Governance policy source is pluggable — default is file; you can implement cloud-backed sources.
- Telemetry contexts and attributes (`CerbiTopic`) included for downstream routing and analytics.

---

## Performance & benchmark notes
- Baseline (no governance): ~26–28 ns per call (in-memory no-op sink).
- Governance path (validation + redaction): ~0.64–0.71 µs per call (measured with BenchmarkDotNet on representative hardware).
- Measured managed allocations on the governance path: approx840 B per call (reduced by pooling and streaming parsing).
- Benchmarks are in `BenchmarkSuite1/GovernanceLoggingBench.cs`. Run locally with:

```
dotnet run --project BenchmarkSuite1/BenchmarkSuite1.csproj -c Release -- --join
```

These numbers show CerbiStream keeps enforcement cheap while guaranteeing policy compliance across sinks.

---

## Security & compliance
- Policies should be stored and changed via PRs and restricted permissions.
- Redaction is applied at the ingestion point to reduce exposure risk.
- Audit fields (`GovernanceViolations`, `GovernanceProfileVersion`) make it easy to prove enforcement for audits.
- Encryption support (AES) for file fallback and optional payload encryption for sensitive storage.

---

## Packaging & CI
- NuGet packaging metadata is configured in `LoggingStandards/CerbiStream.csproj` (symbols/snupkg generation enabled).
- GitHub Actions workflow at `.github/workflows/build-and-test.yml` builds and runs tests on push/PR.

---

## How CerbiStream fits the Cerbi product family
- Runtime enforcement is complemented by static analysis via `CerbiStream.GovernanceAnalyzer` and dashboards such as `CerbiShield`.
- `CerbIQ` and analytics products benefit from standardized, redacted, and tagged logs for ML/AI use.

---

## Selling points (clear value props for stakeholders)
- Reduce risk: prevents accidental PII leakage and reduces remediation costs.
- Compliance-first: enforces policies at the earliest point, simplifying audits.
- Cost control: standardized logs reduce high-cardinality fields being stored/indexed in multiple systems.
- Developer-friendly: integrates with existing logging frameworks — no need to replace Serilog or MEL.
- ML/AI readiness: consistent schemas and enforced metadata make logs immediately usable for analytics and models.
- Fast and safe: sub-microsecond enforcement for typical cases with low allocation strategies.

---

## FAQ (short)
Q: Does CerbiStream replace Serilog or MEL?
A: No. CerbiStream is a governance/enrichment layer that plugs into MEL/Serilog. It ensures logs are compliant before they reach sinks.

Q: What if policy changes frequently?
A: The adapter watches the policy file and reloads safely; for remote policy sources you can implement a custom source.

Q: What if I need zero-latency logging?
A: For extreme use cases you can offload validation to a background pipeline (trade-off: immediate delivery semantics change). Contact us to help design that architecture.

---

## Documentation & support
- Installation & quick start: `docs/INSTALLATION.md`
- Production guidance & checklist: `docs/README-PRODUCTION.md`
- Troubleshooting: `docs/TROUBLESHOOTING.md`
- Technical walkthrough: `docs/WALKTHROUGH-TECHNICAL.md`
- Non-technical overview: `docs/OVERVIEW-NONTECHNICAL.md`

---

## Contributing
Contributions are welcome. Please follow existing code style, add tests for behavior changes, and run the benchmark suite when changing hot paths.

---

If you want this content split into a dedicated `docs/FEATURES.md` or `docs/WHY-CERBI.md`, I can move it and add cross-links. What would you prefer?
