# Release Notes

Use this file to track changes between releases.

## 2.0.5 - 2026-06-24
- **Security: Fix OpenTelemetry CVE chain (issues #28-32)** — Resolved NU1605 package downgrade errors caused by OpenTelemetry 1.15.3 pulling in `Microsoft.Extensions.Logging.Configuration 10.0.0`, which requires `Microsoft.Extensions.Configuration.Abstractions >= 10.0.0` and `Microsoft.Extensions.Configuration >= 10.0.0`. Upgraded the following packages from 9.0.8 → 10.0.0:
  - `Microsoft.Extensions.Configuration`
  - `Microsoft.Extensions.Configuration.Abstractions`
  - `Microsoft.Extensions.Hosting.Abstractions`
- OpenTelemetry 1.15.3 and OpenTelemetry.Exporter.Console 1.15.3 are now fully compatible with no transitive dependency conflicts.

## 2.0.6 - 2026-07-11
- **Security: remediate CVE-2026-40894 (issue #32)** — Upgraded OpenTelemetry and OpenTelemetry.Exporter.Console from 1.15.3 to 1.16.0 so downstream resolution no longer pulls vulnerable OpenTelemetry.Api 1.13.0 transitively.

## 1.1.22 - 2026-01-01
- Security: Added an explicit dependency on `System.Text.Encodings.Web` 8.0.0 to remediate CVE-2021-26701 across downstream consumers.
