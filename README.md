# CerbiStream — Governance-Enforced, PII-Safe Logging for .NET

CerbiStream is a **governance and safety layer** for .NET logging. It validates, redacts, tags, and optionally encrypts logs **before they reach any sink**.

Keep your existing stack:

- `Microsoft.Extensions.Logging` (MEL)
- Serilog
- NLog
- log4net
- OpenTelemetry / OTLP exporters

…and add **policy-driven safety, consistency, and ML-ready metadata** on top.

---

## 🔑 Key Features

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
- Works on structured payloads so you don’t leak values to downstream sinks.

### Runtime validation

- Backed by **`Cerbi.Governance.Runtime`**.
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
- Minimal “dev mode” & “benchmark mode” for hot-path tuning.
- Benchmarks show **parity with established loggers** on baseline scenarios.

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

CerbiStream is **not** trying to replace Serilog/NLog/OTEL. It’s a **governance layer in front of them**.

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

---

## ⚡ Quickstart (≤ 60 seconds)

### 1) Install the package

```powershell
Install-Package CerbiStream
# or
dotnet add package CerbiStream
````

### 2) Add a minimal governance profile `cerbi_governance.json`

Put this next to your app executable (or adjust `configPath`):

```json
{
  "Version": "1.0.0",
  "LoggingProfiles": {
    "default": {
      "DisallowedFields": [ "ssn", "creditCard" ],
      "FieldSeverities": {
        "password": "Forbidden"
      }
    }
  }
}
```

### 3) Wire CerbiStream into Microsoft.Extensions.Logging

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CerbiStream.Configuration; // AddCerbiStream / AddCerbiGovernanceRuntime

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();

        // Option A: Wrap an existing factory with governance runtime
        var innerFactory = LoggerFactory.Create(b => b.AddConsole());
        logging.AddCerbiGovernanceRuntime(
            innerFactory,
            profileName: "default",
            configPath: "./cerbi_governance.json");

        // Option B: Opinionated CerbiStream registration with options
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

        // Optional: CerbiStream-driven health + metrics
        logging.AddCerbiStreamHealthChecks();
    })
    .Build();

await host.RunAsync();
```

### 4) Log as usual

```csharp
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("User signup", new
{
    email = "a@b.com",
    ssn   = "111-11-1111"
});
```

CerbiStream will **redact** disallowed/forbidden fields and **add governance tags** before any sink sees the event.

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

## 🧾 Governance Profile (JSON) Template

```json
{
  "Version": "1.0.0",
  "LoggingProfiles": {
    "default": {
      "RequiredFields": [ "message", "timestamp" ],
      "ForbiddenFields": [ "password" ],
      "DisallowedFields": [ "ssn", "creditCard" ],
      "FieldSeverities": {
        "password": "Forbidden",
        "creditCard": "Forbidden"
      },
      "SensitiveTags": [ "PII", "Secret" ],
      "Encryption": {
        "Mode": "AES",
        "RotateEncryptedFiles": true
      }
    }
  }
}
```

Notes:

* `DisallowedFields` and any field with severity `Forbidden` will be redacted.
* `RequiredFields` are validated and surfaced as violations when missing.
* Profiles are **just JSON** – keep them in Git, and let Cerbi’s file watcher hot-reload changes.

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

**Run the repo’s benchmarks:**

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

You don’t need a **CerbiStream.Fluentd** or **CerbiStream.Alloy** NuGet package.
You need: **CerbiStream in-process**, plus configuration for your collector/exporter to ingest those governed logs.

---

## ❓ FAQ

**Does this replace Serilog or NLog?**
No. CerbiStream is a **governance layer**, not a sink library. Keep Serilog/NLog/OTEL; add CerbiStream to enforce profiles and redaction before events flow into those stacks.

---

**What about performance overhead?**
CerbiStream is designed to be **competitive with top loggers**. Baseline cost is close to raw MEL; governance/redaction cost is explicit and measurable in the included benchmarks.

---

**What happens when governance is disabled or relaxed?**

* When **disabled**, CerbiStream behaves like a thin pass-through provider.
* When **`GovernanceRelaxed = true`**, enforcement is skipped for that entry:

  * No redaction
  * Event is tagged as relaxed for downstream scoring

---

**Can I manage governance profiles centrally?**
Yes. Profiles can be generated and deployed via **CerbiShield** (governance dashboard) and consumed by CerbiStream, MEL plugins, and Serilog governance adapters.

---

## ✅ Call to Action

* ⭐ **Star the repo** if CerbiStream helps keep your logs safe and compliant.
* 🧪 Use it side-by-side with your existing logger to evaluate governance impact.
* 💬 Open issues for:

  * Additional examples (Fluentd, Alloy, Loki, OTEL Collector configs)
  * Feature requests
  * Benchmark scenarios you care about

---

## 📚 Appendix: .NET Logging Governance Topics (SEO)

* .NET logging governance
* PII-safe logging for .NET
* Runtime log redaction for C#
* Policy-driven structured logging
* Governance profiles for Serilog, NLog, MEL
* OpenTelemetry logging with PII enforcement
* OTEL Collector with governed logs
* AES-encrypted log files for .NET
* CerbiStream vs Serilog vs NLog vs log4net
