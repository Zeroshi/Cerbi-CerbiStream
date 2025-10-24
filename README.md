# CerbiStream: Dev-Friendly, Governance-Enforced Logging for .NET

*Brought to you by **Cerbi LLC**, your trusted partner in enterprise observability.*

> 🚀 **[View CerbiStream Benchmarks](https://cerbi.io)**
> Compare against Serilog, NLog, and others. CerbiStream is engineered for high performance, strict governance, and enterprise-grade log routing.

[![CerbiStream NuGet](https://img.shields.io/nuget/v/CerbiStream?label=CerbiStream%20NuGet&style=flat-square)](https://www.nuget.org/packages/CerbiStream/)
[![CerbiStream Downloads](https://img.shields.io/nuget/dt/CerbiStream?label=Downloads&style=flat-square)](https://www.nuget.org/packages/CerbiStream/)
[![Governance Analyzer NuGet](https://img.shields.io/nuget/v/CerbiStream.GovernanceAnalyzer?label=Governance%20Analyzer%20NuGet&style=flat-square)](https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer/)
[![Governance Analyzer Downloads](https://img.shields.io/nuget/dt/CerbiStream.GovernanceAnalyzer?label=Governance%20Analyzer%20Downloads&style=flat-square)](https://www.nuget.org/packages/CerbiStream.GovernanceAnalyzer/)
[![Build Status](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml/badge.svg?branch=master)](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/cerbi-devsecops.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Zeroshi_Cerbi-CerbiStream&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Zeroshi_Cerbi-CerbiStream)

[![Benchmark Tests Repo](https://img.shields.io/badge/View-Benchmark%20Tests-blue?style=flat-square)](https://github.com/Zeroshi/CerbiStream.BenchmarkTests)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

---

## Quick Links
- Installation and setup: docs/INSTALLATION.md
- Production operations: docs/README-PRODUCTION.md
- Troubleshooting: docs/TROUBLESHOOTING.md
- Technical walkthrough: docs/WALKTHROUGH-TECHNICAL.md
- Overview for non-technical stakeholders: docs/OVERVIEW-NONTECHNICAL.md

---

## 🔗 Supported Destinations

CerbiStream can route logs to:

* Queues: Azure Service Bus, RabbitMQ, Kafka, AWS SQS/Kinesis, Google Pub/Sub
* HTTP Endpoints: Any REST API with custom headers
* Cloud Storage: Azure Blob, AWS S3, Google Cloud Storage
* File Fallback: Local JSON file (AES/Base64 encryption supported)
* Telemetry Providers: App Insights, OpenTelemetry, Datadog, AWS CloudWatch, GCP Stackdriver

---

## 🧱 CerbiSuite Overview

| Component | Purpose |
| ---------------------------------- | ------------------------------------------------------ |
| CerbiStream | Structured logging for .NET with queue & cloud targets |
| Cerbi.Governance.Runtime | Runtime enforcement of governance rules |
| CerbiStream.GovernanceAnalyzer | Compile-time governance analyzer |
| CerbiShield (coming soon) | Governance dashboard & deployment portal |
| CerbIQ (coming soon) | Metadata aggregation + routing pipeline |
| CerbiSense (coming soon) | Governance scoring & ML analysis engine |

---

## Developer Quick Start

See docs/INSTALLATION.md for end-to-end setup including policy file and runtime registration.

Minimal setup:

```
var inner = LoggerFactory.Create(b => b.AddConsole());
builder.Logging.AddCerbiGovernanceRuntime(inner, "default");
```

Policy file example (`cerbi_governance.json`):

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

---

## 🆕2025 Governance Update

CerbiStream now uses real-time governance enforcement via Cerbi.Governance.Runtime.

### Runtime Advantages:

* Compatible with .NET6–8+
* Config from local, blob, or GitHub
* Automatically tags logs with governance info
* Supports `.Relax()` and `[CerbiTopic]`

---

## Features

- Topic-based log scoping
- Metadata injection
- Governance policy validation
- Relaxed log support
- Queue & blob transport
- Polly retry support

---

## Unit Testing

- Governance rule enforcement
- Retry/backoff
- Metadata injection
- Fallback logging rotation
- Encryption and validation

---

## License

MIT © Cerbi LLC
