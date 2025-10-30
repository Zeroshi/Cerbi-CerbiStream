# CerbiStream — Governance-Enforced Structured Logging for .NET

CerbiStream helps teams produce safe, standardized, and ML-ready logs. It enforces governance policies at runtime (redaction, tagging, validation) before logs reach any sink, while integrating with existing logging frameworks such as `Microsoft.Extensions.Logging` and Serilog.

This README is ordered for new users: quick overview, why it matters, how to get started, key features, integrations, performance, security & compliance, docs and troubleshooting, and value props.

---

## SDK & Language
- SDK pinned via `global.json` to .NET9 (`9.0.x`, rollForward=latestFeature, allowPrerelease=true). Plan to bump to `10.0.100` post-GA.
- C# language version centralized to `latest` and `Nullable` is enabled via `Directory.Build.props`.
- Target frameworks are unchanged; library and apps continue to target `net8.0`.

## Quick summary (What is CerbiStream?)
- A runtime logging layer that validates, tags, and redacts structured logs according to a policy.
- Works as a wrapper around your existing logging pipeline (MEL, Serilog adapters available).
- Keeps logging fast and consistent for downstream analytics and ML.

---

## Developer-friendly additions (recent)
Enabled by default or via simple options:

- `AddCerbiStream` convenience registration
 - Overloads:
 - `AddCerbiStream(this ILoggingBuilder, Action<CerbiStreamOptions>)` — configure via fluent options.
 - `AddCerbiStream(this ILoggingBuilder)` — opinionated defaults.
 - Registers `CerbiStreamOptions`, `CerbiStreamLoggerProvider`, `RuntimeGovernanceValidator`, health helper, and (when configured) file fallback + rotation.
 - Registers `IEncryption` via `EncryptionFactory` based on options.

- `HealthHostedService`
 - Tiny hosted service that checks for presence/accessibility of the governance policy file at startup and logs warnings/info.

- Telemetry & metadata helpers
 - `TelemetryContext` snapshot facility and enrichment in adapters when enabled.
 - Lightweight enrichment of tracing identifiers (`TraceId`, `SpanId`) when tracing enrichment is on.

- Relaxed logging helper
 - `logger.Relax()` wrapper allows marking specific logs as `GovernanceRelaxed` (bypass enforcement) for intentional diagnostics.

- Performance-friendly runtime changes
 - Governance adapter pools temporary `Dictionary<string, object>` and `HashSet<string>` to reduce allocations.
 - Streaming parsing of JSON-formatted `GovernanceViolations` via `Utf8JsonReader`.
 - The governance logger provider now uses a fresh dictionary for structured state to ensure sinks can safely read/redact state.

- Tests
 - Unit tests cover options, governance behaviors, telemetry providers, health hosted service, and wiring integration.

Quick usage example (recommended):

```csharp
var host = Host.CreateDefaultBuilder(args)
 .ConfigureLogging(logging =>
 {
 logging.ClearProviders();
 logging.AddConsole();

 // Option A: Governance wrapper over an inner factory (keeps your sinks there)
 var innerFactory = LoggerFactory.Create(b => b.AddConsole());
 logging.AddCerbiGovernanceRuntime(innerFactory, profileName: "default", configPath: "./cerbi_governance.json");

 // Option B: Opinionated registration with options
 logging.AddCerbiStream(options =>
 {
 options
 .WithFileFallback("logs/fallback.json", "logs/primary.json")
 .WithAesEncryption()
 .WithEncryptionKey(
 System.Text.Encoding.UTF8.GetBytes("1234567890123456"),
 System.Text.Encoding.UTF8.GetBytes("1234567890123456"))
 .WithGovernanceChecks(true)
 .WithTelemetryEnrichment(true);
 });

 // Optional: health + metrics middleware for ASP.NET Core
 logging.AddCerbiStreamHealthChecks();
 })
 .Build();
```

This wires CerbiStream into the standard host logging system and registers the health check hosted service automatically. If encryption is enabled, the encrypted file rotation hosted service is registered too.

Run unit tests locally:

```
dotnet test CerbiStream--UnitTests/UnitTests.csproj -f net8.0
```

Re-baseline locally (build, tests, benchmarks):
- Build: `dotnet build -c Release`
- Test: `dotnet test -c Release`
- Benchmarks: `scripts/bench.sh` (Linux/macOS) or `scripts/bench.ps1` (Windows)

Install from NuGet:

```
dotnet add package CerbiStream --version 1.1.20
```

---

## Dev & observability (new)
CerbiStream aims to be developer-friendly and lightweight.

- Built-in metrics
 - Lightweight counters: `LogsProcessed`, `Redactions`, `Violations` in `CerbiStream.Observability.Metrics`.
 - Thread-safe; reset in tests via `Metrics.Reset()`.
 - When a telemetry provider is configured, basic metric events can be forwarded.

- Micro-harness for profiling
 - `MicroHarness` console app exercises the governance logger in a tight loop without BenchmarkDotNet to collect focused profiler traces.

- Prometheus / health endpoints (opt-in)
 - Minimal middleware exposes:
 - `/cerbistream/metrics` — Prometheus-style plaintext metrics.
 - `/cerbistream/health` — basic JSON readiness.
 - Enable in ASP.NET Core with `AddCerbiStreamHealthChecks()` and `UseCerbiStreamMetrics()`.

- Keep it lightweight
 - Everything above is opt-in. The core library has no runtime dependency on ASP.NET Core; middleware uses optional registration.

---

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
1) Add the project reference to `LoggingStandards/CerbiStream.csproj` (or install the NuGet package).
2) Create a policy file `cerbi_governance.json` in your app folder or set `CERBI_GOVERNANCE_PATH`.

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

3) Wire into your logging pipeline:
```csharp
var inner = LoggerFactory.Create(b => b.AddConsole());
builder.Logging.AddCerbiGovernanceRuntime(inner, "default", configPath: "./cerbi_governance.json");
```
OR use the convenience helper:
```csharp
builder.Logging.AddCerbiStream(options =>
{
 options.WithFileFallback("logs/fallback.json", "logs/primary.json");
 // To enable encryption and rotation:
 options.WithAesEncryption()
 .WithEncryptionKey(
 System.Text.Encoding.UTF8.GetBytes("1234567890123456"),
 System.Text.Encoding.UTF8.GetBytes("1234567890123456"));
});
```
4) Run. Logs that include `ssn` or `creditCard` fields will be redacted as `***REDACTED***` and governance tags will be present.

For more: see `docs/INSTALLATION.md` and `docs/README-PRODUCTION.md`.

---

## Key features (at a glance)
- Runtime governance enforcement (validate, tag, redact)
- Profile-based policies (`LoggingProfiles`) and env override via `CERBI_GOVERNANCE_PATH`
- In-place, case-insensitive redaction for structured logs
- Relaxed mode (`GovernanceRelaxed`) to bypass enforcement when intentional
- Low-latency with allocation-conscious internals (adapter pooling and streaming parsing)
- Integrations with AppInsights, OpenTelemetry, Datadog, AWS CloudWatch, GCP Stackdriver
- Queue + storage sinks: Azure, AWS SQS/Kinesis/S3, Google Pub/Sub/Storage, RabbitMQ, Kafka
- File fallback with optional encryption (AES/Base64) and rotation
- Configurable retry policies and telemetry enrichment
- Unit tests and benchmark suite included (`CerbiStream--UnitTests`, `BenchmarkSuite1`, `MicroHarness`)

---

## Performance & benchmark notes
- Baseline (no governance): in-memory no-op sink is near-constant time.
- Governance path (validation + redaction): microsecond-range on typical hardware.
- Benchmarks are in `BenchmarkSuite1`. You can also use `MicroHarness` for focused profiling without harness overhead.

---

## Security & compliance
- Policies should be stored and changed via PRs and restricted permissions.
- Redaction is applied at ingestion to reduce exposure.
- Audit fields (`GovernanceViolations`, `GovernanceProfileVersion`) aid compliance.
- Encryption support (AES/Base64) for file fallback and optional payload encryption.

---

## Packaging & CI
- NuGet packaging metadata is in `LoggingStandards/CerbiStream.csproj`.
- GitHub Actions workflow `.github/workflows/build-and-test.yml` builds and runs tests on push/PR.

---

## FAQ (short)
Q: Does CerbiStream replace Serilog or MEL?
A: No. CerbiStream is a governance/enrichment layer that plugs into MEL/Serilog.

Q: What if policy changes frequently?
A: The adapter watches the policy file and reloads safely; for remote sources implement a custom provider.

Q: What if I need zero-latency logging?
A: Consider bypass/relaxed flows or background validation depending on requirements.

---

## Documentation & support
- Installation & quick start: `docs/INSTALLATION.md`
- Production guidance & checklist: `docs/README-PRODUCTION.md`
- Troubleshooting: `docs/TROUBLESHOOTING.md`
- Technical walkthrough: `docs/WALKTHROUGH-TECHNICAL.md`
- Non-technical overview: `docs/OVERVIEW-NONTECHNICAL.md`

---

## Contributing
Contributions are welcome. Please follow existing code style, add tests, and run the benchmark suite when changing hot paths.
