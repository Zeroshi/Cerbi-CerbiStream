# CerbiStream v2.0 — Developer-First Logging Governance for .NET

[![cerbi.io](https://img.shields.io/badge/cerbi.io-Visit%20Website-blue?style=for-the-badge)](https://cerbi.io)
[![NuGet](https://img.shields.io/nuget/v/CerbiStream?style=for-the-badge&color=green)](https://www.nuget.org/packages/CerbiStream)
[![Downloads](https://img.shields.io/nuget/dt/CerbiStream?style=for-the-badge)](https://www.nuget.org/packages/CerbiStream)

**CerbiStream v2.0** is the **developer-first governance layer** for .NET logging. One line of code gives you PII protection, automatic redaction, and enterprise-grade compliance.

**Targets .NET 8.0, .NET 9.0, and .NET 10.0.**

```csharp
// That's it! One line to secure your logs.
builder.Logging.AddCerbiStream();
```

---

## 🚀 What's New in v2.0

### Developer-First Experience
- **One-line setup** — `AddCerbiStream()` just works with zero configuration
- **Auto-generated governance policy** — Sensible PII defaults created automatically  
- **Preset modes** — `EnableDeveloperMode()`, `ForProduction()`, `ForTesting()`, `ForPerformance()`

### Built-in Sensitive Field Detection (NEW!)
- **11 high-confidence PII patterns** detected automatically — `password`, `secret`, `apikey`, `ssn`, `creditcard`, and more
- **Zero-config protection** — works even without a governance profile
- **Value from day zero** — install, log, and immediately get governance feedback
- **Customer overrides** — profile-level settings always take precedence over built-in defaults

### Environment Variable Configuration
- **Zero-code deployments** — Same code works everywhere, controlled by environment
- **Instant debugging** — Enable console output in production without redeploying
- **Kubernetes/Docker native** — 20+ environment variables for complete control

### Enterprise Features
- **Azure App Insights integration** — Built-in telemetry provider
- **Queue scoring** — Send governance metadata to queues for analytics
- **Encrypted file fallback** — AES-256 encrypted local logs when queues fail
- **Hot-reload governance** — Policy changes apply instantly without restart

---

## 🔑 Key Features

### 🛡️ Built-in Sensitive Field Detection

CerbiStream automatically detects common sensitive fields in your logs via `SensitiveFieldCatalog` — **no governance profile needed**. This provides instant value from the moment you install the package.

**11 built-in patterns**: `password`, `secret`, `accesstoken`, `refreshtoken`, `authtoken`, `bearertoken`, `apikey`, `connectionstring`, `privatekey`, `ssn`, `creditcard`

Field names are normalized (lowercase, strip hyphens/underscores) so `AccessToken`, `access_token`, and `access-token` all match.

```csharp
// Install CerbiStream → log with sensitive fields → get instant governance feedback
builder.Logging.AddCerbiStream();

logger.LogInformation("Login attempt {password} {userId}", "secret123", "u-123");
// → GovernanceViolation: "password" matched built-in sensitive field pattern
// → No profile configuration needed!
```

Profile-level configuration **always takes precedence** — if your profile explicitly addresses a field, the built-in default is skipped.

### Governance rules (runtime enforcement)

- Validate log payloads against a **governance profile** (`cerbi_governance.json`).
- Tag events with:
  - `GovernanceViolations`
  - `GovernanceProfileVersion`
  - `GovernanceRelaxed`
- Case-insensitive matching for forbidden/disallowed fields.

### Redaction

- Automatic **in-place redaction** of:
  - `DisallowedFields`
  - Fields with severity `Forbidden`
- Works on structured payloads so you don't leak values to downstream sinks.

### Runtime validation

- Backed by **`Cerbi.Governance.Runtime`** v2.0.23.
- File watcher for **hot-reloading governance profiles** when `cerbi_governance.json` changes.
- Consistent behavior across CerbiStream, Cerbi.MEL.Governance, and Serilog/MEL plugins.

### Analyzer integration

Pair CerbiStream with Cerbi analyzers to **catch issues before runtime**:

- Lint for risky fields (e.g., `password`, `ssn`, `creditCard`).
- Enforce required context and schemas during development.
- Shift PII problems left into CI and IDEs.

### Performance

- Allocation-aware adapter:
  - Pooled dictionaries for structured state
  - Streaming JSON parsing (`Utf8JsonReader`) for violation fields
- Minimal "dev mode" & "benchmark mode" for hot-path tuning.
- Benchmarks show **parity with established loggers** on baseline scenarios.
- Built-in sensitive field catalog uses **static readonly arrays** — zero allocation after initialization.

### Encryption

- Optional **AES/Base64** encryption for **file fallback logs**.
- Encrypted file rotation service for:
  - `max size`
  - `max age`
- Centralized encryption mode selection via Cerbi options.

### ML-ready metadata

- Consistent, structured fields:
  - `GovernanceViolations`
  - `GovernanceProfileVersion`
  - `GovernanceRelaxed`
  - Environment/instance tags
- Makes downstream queries and ML features **predictable and repeatable** across tools (Loki, Seq, ELK/OpenSearch, Graylog, VictoriaLogs, OpenObserve, etc.).

---

## 🤔 Why CerbiStream vs Serilog / NLog / OpenTelemetry?

CerbiStream is **not** trying to replace Serilog/NLog/OTEL. It's a **governance layer in front of them**.

- **Serilog / NLog / log4net**
  - Great at structured logging and sink ecosystems.
  - Do **not** enforce:
    - Required fields
    - Forbidden fields
    - Runtime redaction driven by governance profiles

- **OpenTelemetry (OTEL)**
  - Great at telemetry pipelines and exporters (OTLP, OTEL Collector, Prometheus, etc.).
  - Does **not** enforce policy-based PII rules on application payloads.

**CerbiStream complements these:**

- Validates/marks/redacts logs **before**:
  - Serilog sinks
  - NLog targets
  - OTEL exporters / Collector
  - Loki / Seq / ELK / Graylog / VictoriaLogs / OpenObserve / TelemetryHarbor / Fluentd / Alloy / syslog

Use CerbiStream when:

- You need **.NET logging governance** with explicit profiles and enforcement.
- You must guarantee **PII-safe logging** *before* data leaves the process.
- You want **runtime validation** plus **analyzer-time enforcement**.
- You prefer **safe defaults** with opt-in relaxation for diagnostics.

## 🧪 Demo API for hands-on testing

Want to see CerbiStream governance in action without wiring up your own project? Try the public demo API built for quick evaluation:

- Repository: [Cerbistream.Governance.Demo.API](https://github.com/Zeroshi/Cerbistream.Governance.Demo.API)
- Includes ready-to-run .NET API endpoints that emit governed logs using CerbiStream.
- Pair it with the demo's `cerbi_governance.json` to watch runtime validation and redaction behaviors end-to-end.

---

## ⚡ Quickstart (One Line!)

### 1) Install

```bash
dotnet add package CerbiStream
```

### 2) Add to your app

```csharp
// Program.cs - That's it! One line!
builder.Logging.AddCerbiStream();
```

**Done!** You now have:
- ✅ PII protection (passwords, SSNs, credit cards auto-detected via built-in catalog)
- ✅ 11 sensitive field patterns active out of the box
- ✅ Governance policy auto-generated
- ✅ Console output for development
- ✅ **Auto-detects environment variables** for zero-code config changes
- ✅ Ready for production upgrade

### 3) Log as usual

```csharp
// Just use standard ILogger - CerbiStream handles the rest
logger.LogInformation("User signup {email} {ssn}", "a@b.com", "111-11-1111");
// Output: ssn is automatically redacted to "***REDACTED***"
```

---

## 🎯 Configuration Presets

```csharp
// Development (default) — Console on, queue off, governance on
builder.Logging.AddCerbiStream();

// Production — Full governance, telemetry, queue enabled
builder.Logging.AddCerbiStream(o => o.ForProduction());

// Testing — Governance on, no external dependencies
builder.Logging.AddCerbiStream(o => o.ForTesting());

// Performance — All enrichment disabled for benchmarks
builder.Logging.AddCerbiStream(o => o.ForPerformance());
```

| Preset | Console | Queue | Governance | Telemetry |
|--------|---------|-------|------------|----------|
| `EnableDeveloperMode()` | ✅ | ❌ | ✅ | ❌ |
| `ForProduction()` | ❌ | ✅ | ✅ | ✅ |
| `ForTesting()` | ✅ | ❌ | ✅ | ❌ |
| `ForPerformance()` | ❌ | ❌ | ❌ | ❌ |

---

## 🌍 Environment Variable Configuration

**Zero code changes** — deploy the same code everywhere, control behavior with environment variables.

### Quick Mode Switch

```bash
# Linux/Mac
export CERBISTREAM_MODE=production

# Windows PowerShell
$env:CERBISTREAM_MODE = "production"

# Docker
docker run -e CERBISTREAM_MODE=production myapp

# Kubernetes
env:
  - name: CERBISTREAM_MODE
    value: "production"
```

### All Environment Variables

| Variable | Values | Description |
|----------|--------|-------------|
| `CERBISTREAM_MODE` | `development`, `production`, `testing`, `performance` | Master preset switch |
| `CERBISTREAM_GOVERNANCE_ENABLED` | `true`/`false` | Toggle PII redaction |
| `CERBISTREAM_GOVERNANCE_PROFILE` | Profile name | e.g., `myapp`, `default` |
| `CERBI_GOVERNANCE_PATH` | File path | Path to governance JSON |
| `CERBISTREAM_QUEUE_ENABLED` | `true`/`false` | Toggle queue sending |
| `CERBISTREAM_QUEUE_TYPE` | `AzureServiceBus`, `RabbitMQ`, `Kafka`, etc. | Queue provider |
| `CERBISTREAM_QUEUE_CONNECTION` | Connection string | Queue connection |
| `CERBISTREAM_QUEUE_NAME` | Queue name | Target queue/topic |
| `CERBISTREAM_ENCRYPTION_MODE` | `None`, `Base64`, `AES` | Encryption type |
| `CERBISTREAM_CONSOLE_OUTPUT` | `true`/`false` | Console logging |
| `CERBISTREAM_TELEMETRY_ENABLED` | `true`/`false` | Telemetry sending |
| `CERBISTREAM_FILE_FALLBACK_ENABLED` | `true`/`false` | File fallback |

### Debug Production Issues Instantly

```bash
# Enable console output without redeploying
kubectl set env deployment/myapp CERBISTREAM_CONSOLE_OUTPUT=true

# Disable queue temporarily
kubectl set env deployment/myapp CERBISTREAM_QUEUE_ENABLED=false
```

### Layered Configuration

Environment variables + code config work together:

```csharp
// Start from environment, then override specific settings
builder.Logging.AddCerbiStream(o => o
    .FromEnvironment()                    // Load from env vars
    .WithGovernanceProfile("override"));  // Code takes precedence
```

---

## 🔧 Advanced Configuration

```csharp
builder.Logging.AddCerbiStream(options => options
    .ForProduction()
    .WithGovernanceProfile("myservice")
    .WithQueueRetries(true, retryCount: 5, delayMilliseconds: 500)
    .WithFileFallback("logs/fallback.json", "logs/primary.json")
    .WithAesEncryption()
    .WithEncryptionKey(key, iv)
    .WithTelemetryProvider(myTelemetryProvider));
```

### Governance Runtime & Analyzer Compatibility

| TFM      | CerbiStream package | Cerbi.Governance.Core | Cerbi.Governance.Runtime | CerbiStream.GovernanceAnalyzer |
|----------|---------------------|-----------------------|--------------------------|--------------------------------|
| net8.0   | latest              | 2.2.29                | 2.0.23                   | latest                         |
| net9.0   | latest              | 2.2.29                | 2.0.23                   | latest                         |
| net10.0  | latest              | 2.2.29                | 2.0.23                   | latest                         |

All packages now use the canonical `Profile` model and `CerbiShield.Contracts v1.2.1` for consistent governance enforcement across the entire ecosystem.

---

## 🔍 Governance Example: Before vs After

**Before (unsafe):**

```json
{
  "message": "User signup",
  "email":   "a@b.com",
  "ssn":     "111-11-1111"
}
```

**After (governed by CerbiStream):**

```json
{
  "message": "User signup",
  "email": "a@b.com",
  "ssn": "***REDACTED***",
  "GovernanceViolations": [
    { "Code": "ForbiddenField", "Field": "ssn" }
  ],
  "GovernanceProfileVersion": "1.0.0"
}
```

**Opt-in relaxation for intentional diagnostics:**

```csharp
logger.LogInformation("debug payload", new
{
    GovernanceRelaxed = true,
    dump = secretPayload
});
```

When `GovernanceRelaxed = true` and your profile allows relax, CerbiStream **skips enforcement/redaction** for that entry but still tags it as relaxed for downstream scoring and audit.

---

## 🧾 Governance Profile (Canonical JSON)

```json
{
  "name": "PII Protection",
  "appName": "my-service",
  "version": "1.0.0",
  "status": "Published",
  "metadata": {
    "description": "Prevents PII leakage in application logs",
    "owner": "security-team"
  },
  "requiredFields": ["message", "timestamp"],
  "disallowedFields": ["ssn", "creditCard"],
  "fieldSeverities": {
    "password": "Forbidden",
    "creditCard": "Forbidden"
  },
  "encryption": {
    "mode": "AES",
    "encryptedFields": ["ssn", "email"]
  },
  "allowRelax": false
}
```

Notes:

* `disallowedFields` and any field with severity `Forbidden` will be redacted.
* `requiredFields` are validated and surfaced as violations when missing.
* Profiles are **just JSON** – keep them in Git, and let Cerbi's file watcher hot-reload changes.
* Built-in sensitive field detection provides a safety net even without a profile.

---

## 📈 Performance

CerbiStream includes a **Benchmark & Evaluation suite** that compares it to:

* Microsoft.Extensions.Logging (MEL)
* Serilog
* NLog
* log4net

**Baseline summary (Release, .NET 8, no-op sinks):**

| Scenario               | Relative throughput |
| ---------------------- | ------------------- |
| Baseline (MEL console) | 1.00x               |
| Serilog console        | 0.95x–1.05x         |
| NLog console           | 0.90x–1.00x         |
| CerbiStream + console  | ~0.90x–0.98x        |

What makes it fast:

* Allocation-aware adapter with:

  * Pooled `Dictionary<string, object>`
  * Pooled `HashSet<string>`
* Streaming parse of governance metadata via `Utf8JsonReader`
* Immediate short-circuit when `GovernanceRelaxed` is set
* SensitiveFieldCatalog uses static readonly arrays — zero allocation per-request

**Run the repo's benchmarks:**

* Windows: `scripts/bench.ps1`
* Linux/macOS: `scripts/bench.sh`
* Or directly:

```bash
dotnet run --project Cerbi-Benchmark-Tests/Cerbi-Benchmark-Tests.csproj -c Release
```

For full benchmark commentary, see the **CerbiStream Benchmark & Evaluation Suite** README in this repo.

---

## 🔗 Integration Patterns

* **MEL**
  Primary integration via `AddCerbiStream` / `AddCerbiGovernanceRuntime`.

* **Serilog**
  Wrap your Serilog-backed `ILoggerFactory` so Cerbi governance runs **before** Serilog sinks.

* **NLog / log4net**
  Integrate via MEL or by routing governed events into existing targets.

* **OpenTelemetry**
  Use CerbiStream in the app, then export via OTLP to the OTEL Collector. Logs arrive already governed/redacted.

* **Azure Container Apps (ACA) / Kubernetes**
  CerbiStream is fully compatible with containerized .NET apps:

  - **Environment variables**: Set `CERBI_GOVERNANCE_PATH=/app/config/cerbi_governance.json` to override the default location.
  - **ConfigMaps / Volumes**: Mount your governance profile as a read-only volume; the library's `FileSystemWatcher` gracefully degrades on read-only mounts, falling back to timestamp-based reload checks.
  - **AppContext.BaseDirectory**: Falls back to `./cerbi_governance.json` next to the app executable when `CERBI_GOVERNANCE_PATH` is not set.
  - **Performance**: Pooled dictionaries, HashSets, and streaming JSON parsing ensure minimal allocation overhead at high throughput.
  - **Health checks**: Use `AddCerbiStreamHealthChecks()` to expose `/cerbistream/health` and `/cerbistream/metrics` endpoints for ACA/K8s probes.

  Example for ACA deployment:
  ```yaml
  containers:
    - name: myapp
      image: myregistry.azurecr.io/myapp:latest
      env:
        - name: CERBI_GOVERNANCE_PATH
          value: "/app/config/cerbi_governance.json"
      volumeMounts:
        - name: governance-config
          mountPath: /app/config
          readOnly: true
  volumes:
    - name: governance-config
      secret:
        secretName: cerbi-governance
  ```

* **Downstream stacks**
  CerbiStream plays nicely with:

  * Grafana Loki / Promtail / Alloy
  * Seq
  * ELK / OpenSearch
  * Graylog
  * VictoriaLogs / VictoriaMetrics
  * OpenObserve
  * TelemetryHarbor
  * Fluentd / Fluent Bit
  * Journald / basic syslog + grep/tail

You don't need a **CerbiStream.Fluentd** or **CerbiStream.Alloy** NuGet package.
You need: **CerbiStream in-process**, plus configuration for your collector/exporter to ingest those governed logs.

---

## 📊 CerbiShield Scoring Identity (v1.1)

CerbiStream automatically enriches every `ScoringEventDto` with identity metadata for end-to-end traceability in CerbiShield dashboards.

### Identity Fields

| Field | Source | Purpose |
|-------|--------|---------|
| `ServiceName` | `CerbiStreamOptions.ServiceName` | Logical service name |
| `AppVersion` | `EnvironmentDetector.AppVersion` (auto) | Deployed assembly version |
| `InstanceId` | `EnvironmentDetector.InstanceId` (auto) | Container/pod instance |
| `DeploymentId` | `DEPLOYMENT_ID` env var | Release tracking ID |
| `ProfileName` | Governance profile name | Stamped onto every `ViolationDto` |
| `AppName` | `ServiceName` or log data | Stamped onto every `ViolationDto` |

All identity fields are set automatically via `ScoringEventTransformer.Transform()`. Each `ViolationDto` is stamped with `ProfileName` and `AppName` for downstream linkage to the originating app and governance profile.

This is consistent across all Cerbi SDKs (Serilog, NLog, MEL, CerbiStream).

---

## ❓ FAQ

**Does this replace Serilog or NLog?**
No. CerbiStream is a **governance layer**, not a sink library. Keep Serilog/NLog/OTEL; add CerbiStream to enforce profiles and redaction before events flow into those stacks.

---

**What about performance overhead?**
CerbiStream is designed to be **competitive with top loggers**. Baseline cost is close to raw MEL; governance/redaction cost is explicit and measurable in the included benchmarks. The built-in SensitiveFieldCatalog uses static readonly arrays — zero allocation per request.

---

**What happens when governance is disabled or relaxed?**

* When **disabled**, CerbiStream behaves like a thin pass-through provider.
* When **`GovernanceRelaxed = true`**, enforcement is skipped for that entry:

  * No redaction
  * Event is tagged as relaxed for downstream scoring

---

**Do I need a governance profile to get value?**
No! CerbiStream's built-in `SensitiveFieldCatalog` detects 11 common sensitive field patterns (passwords, API keys, SSNs, credit cards, etc.) with **zero configuration**. Install, log, and immediately get governance feedback. Profiles give you fine-grained control when you're ready.

---

**Can I manage governance profiles centrally?**
Yes. Profiles can be generated and deployed via **CerbiShield** (governance dashboard) and consumed by CerbiStream, MEL plugins, and Serilog governance adapters.

---

## ✅ Test Coverage

**325 tests passing** across:
- 55 integration tests  
- 270 unit tests (135 × 2 frameworks: .NET 8 & .NET 10)

Test categories:
- Zero-config setup
- All preset modes  
- Governance redaction
- Encryption pathways
- Telemetry integration
- Environment variable configuration
- Queue scoring
- File fallback

---

## 🏆 Trusted By

- **Microsoft Partner (ISV)**
- **Harvard Innovation Lab**
- **49.6K+ NuGet downloads**

---

## 📚 Documentation

- [Quick Start Guide](docs/QUICKSTART.md)
- [Installation](docs/INSTALLATION.md)
- [Production Checklist](docs/README-PRODUCTION.md)
- [Technical Walkthrough](docs/WALKTHROUGH-TECHNICAL.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Security](docs/SECURITY.md)

---

## 🔗 Ecosystem

| Package | Version | Purpose |
|---------|---------|--------|
| **CerbiStream** | latest | Core logging governance with built-in sensitive field detection |
| **Cerbi.Governance.Core** | 2.2.29 | Canonical Profile model, SensitiveFieldCatalog, validation helpers |
| **Cerbi.Governance.Runtime** | 2.0.23 | Runtime validation engine with CompiledProfile and scoring |
| **Cerbi.GovernanceAnalyzer** | 1.0.0 | Roslyn analyzer — 9 compile-time diagnostics (CERBI001-009) |
| **Cerbi.Serilog.GovernanceAnalyzer** | latest | Serilog runtime governance enforcement |
| **Cerbi.MEL.Governance** | latest | MEL runtime governance enforcement |
| **Cerbi.NLog.GovernanceAnalyzer** | latest | NLog runtime governance enforcement |
| **CerbiShield** | — | Enterprise governance dashboard |

---

## ✨ Call to Action

* ⭐ **Star the repo** if CerbiStream helps keep your logs safe and compliant.
* 🧪 Use it side-by-side with your existing logger to evaluate governance impact.
* 💬 Open issues for:

  * Additional examples (Fluentd, Alloy, Loki, OTEL Collector configs)
  * Feature requests
  * Benchmark scenarios you care about

---

## 📞 Support

- **GitHub Issues**: [Open an issue](https://github.com/Zeroshi/Cerbi-CerbiStream/issues)
- **Website**: [cerbi.io](https://cerbi.io)
- **Documentation**: [cerbi.io/documents](https://cerbi.io/documents)

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

<p align="center">
  <b>CerbiStream v2.0</b> — Developer-first logging governance for .NET<br>
  <a href="https://cerbi.io">cerbi.io</a> | <a href="https://www.nuget.org/packages/CerbiStream">NuGet</a> | <a href="https://github.com/Zeroshi/Cerbi-CerbiStream">GitHub</a>
</p>
