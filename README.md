# CerbiStream v2.0 — Developer-First Logging Governance for .NET

[![cerbi.io](https://img.shields.io/badge/cerbi.io-Visit%20Website-blue?style=for-the-badge)](https://cerbi.io)
[![NuGet](https://img.shields.io/nuget/v/CerbiStream?style=for-the-badge&color=green)](https://www.nuget.org/packages/CerbiStream)
[![Downloads](https://img.shields.io/nuget/dt/CerbiStream?style=for-the-badge)](https://www.nuget.org/packages/CerbiStream)
[![CerbiSuite](https://img.shields.io/badge/CerbiSuite-Compatible-brightgreen?style=for-the-badge)](https://cerbi.io)

CerbiStream v2.0

Logs are leaking sensitive data in production.

Passwords, tokens, PII — it happens more than teams realize.
Most teams rely on masking in Splunk, Datadog, or downstream pipelines.

That’s already too late.

CerbiStream stops it at the source.

❌ Before (real-world logging)
{
  "message": "User signup",
  "email": "a@b.com",
  "ssn": "111-11-1111"
}
✅ After (CerbiStream enforced)
{
  "message": "User signup",
  "email": "a@b.com",
  "ssn": "***REDACTED***",
  "GovernanceViolations": [
    { "Code": "ForbiddenField", "Field": "ssn" }
  ],
  "GovernanceProfileVersion": "1.0.0"
}

One line to secure your logs:

builder.Logging.AddCerbiStream();

CerbiStream is a developer-first governance layer for .NET logging.

It validates, redacts, and tags logs before they leave your application —
so sensitive data never reaches your logging pipeline in the first place.

👉 Try it in 2 minutes
https://github.com/Zeroshi/Cerbistream.Governance.Demo.API

Most teams only discover logging issues during:

security reviews
compliance audits
or incidents

CerbiStream lets you catch and prevent them during development and at runtime.

🚀 Quickstart
dotnet add package CerbiStream
builder.Logging.AddCerbiStream();

Done.

You now have:

✅ Automatic PII redaction
✅ Governance validation with violation tagging
✅ Safe defaults with zero configuration
✅ Runtime enforcement before logs leave the process
🧠 What CerbiStream Actually Does

CerbiStream sits in front of your existing logger and enforces governance rules:

Validates structured log payloads against a policy
Redacts sensitive fields in-place
Tags violations for downstream analysis
Works with Serilog, NLog, MEL, and OpenTelemetry

It doesn’t replace your logging stack.

It makes it safe and enforceable.

⚡ Example
logger.LogInformation("User signup {email} {ssn}", "a@b.com", "111-11-1111");

Output:

{
  "message": "User signup",
  "email": "a@b.com",
  "ssn": "***REDACTED***",
  "GovernanceViolations": [
    { "Code": "ForbiddenField", "Field": "ssn" }
  ]
}
🔥 When You Should Use CerbiStream

Use CerbiStream if:

You want to guarantee PII never enters logs
You need audit-ready logging behavior
You don’t trust downstream masking
You want runtime + analyzer enforcement
You want safe defaults without slowing developers down
🧩 How It Fits

CerbiStream works alongside your existing tools:

Serilog / NLog → logging + sinks
OpenTelemetry → pipelines + exporters
CerbiStream → governance + enforcement (before all of it)
🔑 Key Features
Governance Rules
Validate logs against cerbi_governance.json
Tag events with violations and metadata
Case-insensitive matching
Redaction
Automatic in-place redaction of sensitive fields
Prevents leakage to downstream sinks
Runtime Validation
Hot-reload governance profiles
Consistent behavior across Cerbi ecosystem
Analyzer Integration
Catch issues at compile-time
Enforce schemas in CI and IDE
Performance
Allocation-aware design
Minimal overhead vs standard loggers
Encryption
AES/Base64 support for file fallback logs
Rotation based on size/age
ML-Ready Metadata
Structured, consistent fields for downstream analysis
🤔 Why CerbiStream vs Serilog / NLog / OpenTelemetry?

CerbiStream is not a replacement.

Serilog / NLog → logging & sinks
OpenTelemetry → pipelines & exporters
CerbiStream → policy enforcement before all of them

Use it when you need:

Guaranteed PII-safe logging
Runtime governance
Enforcement before data leaves the process
🧪 Demo API

Try a real working example:

https://github.com/Zeroshi/Cerbistream.Governance.Demo.API

🎯 Configuration Presets
builder.Logging.AddCerbiStream(); // Dev
builder.Logging.AddCerbiStream(o => o.ForProduction());
builder.Logging.AddCerbiStream(o => o.ForTesting());
builder.Logging.AddCerbiStream(o => o.ForPerformance());
🌍 Environment Variables
export CERBISTREAM_MODE=production

Supports:

Governance toggle
Queue config
Encryption mode
Telemetry
File fallback
🔧 Advanced Configuration
builder.Logging.AddCerbiStream(options => options
    .ForProduction()
    .WithGovernanceProfile("myservice")
    .WithAesEncryption());
🔍 Governance Example

Before:

{
  "email": "a@b.com",
  "ssn": "111-11-1111"
}

After:

{
  "email": "a@b.com",
  "ssn": "***REDACTED***",
  "GovernanceViolations": [
    { "Code": "ForbiddenField", "Field": "ssn" }
  ]
}
📈 Performance
Comparable to Serilog/NLog baseline
Minimal overhead in production scenarios
Benchmarks included in repo
🔗 Integration

Works with:

MEL
Serilog
NLog
OpenTelemetry
Loki, ELK, Seq, etc.
❓ FAQ

Does this replace Serilog or NLog?
No — it enforces governance before them.

Performance impact?
Minimal and benchmarked.

Can governance be disabled?
Yes — behaves like pass-through.

🏆 Trusted By
Microsoft Partner (ISV)
Harvard Innovation Lab
49K+ NuGet downloads
📚 Documentation
Quickstart
Installation
Production Checklist
Technical Walkthrough
📞 Support
GitHub Issues
https://cerbi.io
📄 License

MIT
