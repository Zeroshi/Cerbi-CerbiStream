# Installation and Setup

This guide walks you through installing and wiring CerbiStream for .NET8 applications.

Supported frameworks: .NET8

1) Install NuGet packages
- Application consuming logs typically references the NuGet packages:
 - CerbiStream (main logging package)
 - Cerbi.Governance.Runtime (runtime governance)
- Using CLI:
 - dotnet add package CerbiStream
 - dotnet add package Cerbi.Governance.Runtime

If you are compiling this repository directly, add a project reference to `LoggingStandards/CerbiStream.csproj`.

2) Add governance runtime to logging
Use the provided extension to wrap your logging pipeline with governance validation/redaction.

Example (Program.cs):

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CerbiStream.GovernanceRuntime.Governance;

var builder = Host.CreateApplicationBuilder(args);

// Inner factory holds your sinks/targets (console, providers, etc.)
var innerFactory = LoggerFactory.Create(b =>
{
 b.AddConsole();
 // Add other providers here (AppInsights, Serilog, etc.)
});

// Attach Cerbi governance runtime (profile "default"); path can be null to auto-resolve
builder.Logging.AddCerbiGovernanceRuntime(innerFactory, profileName: "default");

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("demo");

logger.LogInformation("{ssn} {userId}", "111-22-3333", "u1");

app.Run();

3) Provide a governance policy file
Create a JSON file named `cerbi_governance.json` alongside your app or set `CERBI_GOVERNANCE_PATH` to point to it.

Example `cerbi_governance.json`:
{
 "Version": "1.0.0",
 "LoggingProfiles": {
 "default": {
 "DisallowedFields": ["ssn"],
 "FieldSeverities": { "creditCard": "Forbidden" }
 }
 }
}

4) Configure runtime path (optional)
- Environment variable: `CERBI_GOVERNANCE_PATH` can point to an absolute path for your policy file.
- Constructor override: `AddCerbiGovernanceRuntime(..., configPath: "path/to/policy.json")`.

5) Production checklist (quick)
- Use a stable profile name (for example, "default" or your app/service name).
- Keep the policy file in a controlled location and use CI to validate changes.
- Enable file fallback or durable sinks where appropriate.
- Ensure PII fields are listed in DisallowedFields or flagged Forbidden.
- Consider emitting governance tags to your SIEM for auditability.
