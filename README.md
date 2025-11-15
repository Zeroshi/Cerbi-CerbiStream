# CerbiStream — Governance‑Enforced, PII‑Safe Logging for .NET

CerbiStream is a drop‑in governance layer for .NET logging that validates, redacts, tags, and optionally encrypts logs at runtime before they reach any sink. Use your existing pipeline (Microsoft.Extensions.Logging, Serilog adapters) while adding policy‑driven safety, consistency, and ML‑ready metadata.

—

## Key features

- Governance rules (runtime enforcement)
  - Validate payloads against a governance profile; add `GovernanceViolations`, `GovernanceProfileVersion`, and `GovernanceRelaxed` tags.
  - Redact disallowed/forbidden fields in‑place using case‑insensitive matching.
- Redaction
  - Automatic redaction of forbidden/disallowed fields derived from runtime violations and policy (`cerbi_governance.json`).
- Runtime validation
  - Backed by `Cerbi.Governance.Runtime`; hot‑reload policy via file watcher when the profile changes.
- Analyzer integration
  - Pair with Cerbi analyzers to prevent unsafe logging during development (lint for risky fields and missing governance context).
- Performance
  - Allocation‑aware adapter with pooled dictionaries and streaming JSON parsing for violation fields.
  - Minimal dev mode and benchmark mode for hot paths.
- Encryption
  - Optional AES/Base64 for file fallback logs; rotation service for encrypted files.
- ML‑ready metadata
  - Consistent keys and governance tags enable reliable analytics and model features.

—

## Why CerbiStream vs Serilog / NLog / OpenTelemetry?

CerbiStream is not a sink or a general‑purpose logger; it’s a governance and safety layer that sits in front of your sinks.

- Serilog/NLog: excellent structured logging and rich sinks. They don’t enforce governance policies (required fields, forbidden fields, runtime redaction) out of the box. CerbiStream adds policy enforcement and verification across any sinks you already use.
- OpenTelemetry: excellent telemetry pipeline. It does not perform policy‑based field governance or PII enforcement for application logs. CerbiStream complements OTEL by validating/redacting application payloads before export.
- CerbiStream focuses on governance, redaction, and runtime verification so teams can prove “PII‑safe logging” and consistent schemas without replacing their stack.

When to use CerbiStream:
- You need `.NET logging governance` with explicit profiles and enforcement.
- You must guarantee `PII‑safe logging` before data leaves the process.
- You want runtime validation plus analyzer‑time feedback.
- You need safe defaults with opt‑in relaxation for controlled diagnostics.

—

## Quickstart (≤ 60 seconds)

1) Install package

```powershell
Install-Package CerbiStream
# or
 dotnet add package CerbiStream
```

2) Add a minimal governance profile file (next to your app): `cerbi_governance.json`

```json
{
  "Version": "1.0.0",
  "LoggingProfiles": {
    "default": {
      "DisallowedFields": ["ssn", "creditCard"],
      "FieldSeverities": { "password": "Forbidden" }
    }
  }
}
```

3) Wire CerbiStream into Microsoft.Extensions.Logging

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CerbiStream.Configuration; // AddCerbiStream / AddCerbiGovernanceRuntime

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();

        // Option A: Wrap an inner factory with governance
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

        // Optional health + metrics endpoints (ASP.NET Core)
        logging.AddCerbiStreamHealthChecks();
    })
    .Build();

await host.RunAsync();
```

4) Log as usual

```csharp
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("User signup", new { email = "a@b.com", ssn = "111-11-1111" });
```

Result: Disallowed/forbidden fields are redacted, and governance tags are added before any sink processes the log.

—

## Governance example: before vs after

- Before (unsafe):

```json
{"message":"User signup","email":"a@b.com","ssn":"111-11-1111"}
```

- After (governed by CerbiStream):

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

Opt‑in relaxation for intentional diagnostics:

```csharp
logger.LogInformation("debug payload", new { GovernanceRelaxed = true, dump = secretPayload });
```

—

## Governance profile (JSON) template

```json
{
  "Version": "1.0.0",
  "LoggingProfiles": {
    "default": {
      "RequiredFields": ["message", "timestamp"],
      "ForbiddenFields": ["password"],
      "DisallowedFields": ["ssn", "creditCard"],
      "FieldSeverities": {
        "password": "Forbidden",
        "creditCard": "Forbidden"
      },
      "SensitiveTags": ["PII", "Secret"],
      "Encryption": {
        "Mode": "AES",
        "RotateEncryptedFiles": true
      }
    }
  }
}
```

Notes
- `DisallowedFields` and any field with severity `Forbidden` will be redacted.
- `RequiredFields` are validated by the governance runtime and raised as violations when missing.
- Store profiles under version control; Cerbi’s file watcher hot‑reloads updates.

—

## Performance

Benchmark highlights (Release, .NET 8, local dev representative):

| Scenario | Relative throughput |
|---|---|
| Baseline (MEL console) | 1.00x |
| Serilog console | 0.95x–1.05x |
| NLog console | 0.9x–1.0x |
| CerbiStream governance + console | ~0.9x–0.98x |

What makes it fast
- Allocation‑aware adapter with pooled `Dictionary<string,object>` and pooled `HashSet<string>`.
- Streaming parse of `GovernanceViolations` with `Utf8JsonReader` (no `JsonDocument` allocations for strings).
- Short‑circuit for `GovernanceRelaxed`.

Run the repo’s benchmarks
- Windows: `scripts/bench.ps1`
- Linux/macOS: `scripts/bench.sh`
- Or run BenchmarkSuite1 directly: `dotnet run --project BenchmarkSuite1/BenchmarkSuite1.csproj -c Release -- --join --runtimes net8.0`

—

## Integration

- Microsoft.Extensions.Logging (MEL): primary integration via `AddCerbiStream` or `AddCerbiGovernanceRuntime`.
- Serilog: use Cerbi governance runtime to wrap a Serilog‑backed `ILoggerFactory` so governance runs before Serilog sinks.
- OpenTelemetry: continue exporting via OTEL; CerbiStream governs fields before export.
- Runtime enforcement uses `Cerbi.Governance.Core` and `Cerbi.Governance.Runtime` under the hood.

—

## FAQ

- Does this replace Serilog?
  - No. CerbiStream is a governance layer. Keep Serilog/NLog/OTEL; add Cerbi to enforce policies and redaction.

- What about performance?
  - The adapter is allocation‑aware and competitive with top loggers. See benchmark notes above and run the included suite.

- What if governance is disabled or relaxed?
  - If disabled, CerbiStream behaves like a thin provider. If a log sets `GovernanceRelaxed = true`, enforcement/redaction is skipped for that entry.

—

## Call to action

- Star the repo if this helps you build safer logging.
- Open an issue to request integrations (Fluentd, Alloy, Loki, etc.).

—

## Appendix: .NET logging governance topics (SEO)

- .NET logging governance
- PII‑safe logging for .NET
- Runtime log redaction for C#
- Policy‑driven structured logging
- Governance profiles and analyzer‑assisted safety
