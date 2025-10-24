# CerbiStream Production Guide

This document explains how to deploy and operate CerbiStream in production environments with governance runtime enabled.

Audience: Platform/DevOps teams, senior developers.

[![CI](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/Zeroshi/Cerbi-CerbiStream/actions/workflows/build-and-test.yml)

Contents
- Overview and architecture
- Deployment patterns
- Configuration
- Observability
- Troubleshooting
- Operations checklist

## Overview
CerbiStream is a structured logging library with governance enforcement. At runtime it:
- Validates log payloads against an external policy (profile-based)
- Tags logs with governance metadata
- Redacts disallowed/forbidden fields in-place
- Supports relaxed mode for explicitly bypassing checks

Core runtime components:
- GovernanceRuntimeAdapter: applies policy and redaction
- GovernanceLoggerProvider: integrates with ILoggerFactory
- Policy source: cerbi_governance.json (or remote via your own source wrapper)

## Deployment patterns

- Side-by-side factory (recommended)
 - Create an inner LoggerFactory that holds your sinks (console/cloud providers).
 - Wrap the host logging builder with AddCerbiGovernanceRuntime(builder, innerFactory, profile, path).

- Hosted service / middleware
 - Cerbi governance provider can be registered globally during host startup; no app code changes needed beyond logging registration.

## Configuration

- Profile name: choose a profile per application or per domain (e.g., "default", "Payments").
- Policy path resolution:
 - Use `CERBI_GOVERNANCE_PATH` env var; falls back to `./cerbi_governance.json`.
 - Pass explicit path to AddCerbiGovernanceRuntime when needed.
- Policy fields:
 - DisallowedFields: always redacted
 - FieldSeverities: any field severity == "Forbidden" is redacted

Environment variables (production):
- CERBI_GOVERNANCE_PATH=/etc/cerbi/cerbi_governance.json
- ASPNETCORE_ENVIRONMENT=Production

## Observability

- Governance tags present after validation:
 - GovernanceViolations[] (if any)
 - GovernanceProfileVersion
 - GovernanceRelaxed (when bypassed)
- Recommended dashboards include counts of violations and top redacted fields.

## Secure operations

- Check-in policy to a secure repo, gate changes via PR reviews.
- Keep sensitive field names centralized (PII, secrets).
- Validate policy JSON with CI to avoid runtime errors.

## Troubleshooting

- Symptoms: PII not redacted
 - Check profile name mismatch
 - Verify DisallowedFields or FieldSeverities contains the field
 - Confirm governance provider is registered before other providers

- Symptoms: Exception or no effect
 - Validate that `CERBI_GOVERNANCE_PATH` points to an accessible file
 - Ensure the JSON is valid and contains `LoggingProfiles`
 - Enable debug logs to verify GovernanceRuntimeAdapter is invoked

- Symptoms: Policy updates not applied
 - Adapter caches parsed policy by file write time; touch the file to refresh.

## Example
```
var inner = LoggerFactory.Create(b => b.AddConsole());
builder.Logging.AddCerbiGovernanceRuntime(inner, "default", configPath: "/etc/cerbi/cerbi_governance.json");
```

## Operations checklist
See docs/OPERATIONS-CHECKLIST.md for pre/post-deploy checks and ongoing ops.

## Security notice
CerbiStream helps prevent leakage by redacting known forbidden fields. It does not replace application-level secure coding practices or DLP systems.
