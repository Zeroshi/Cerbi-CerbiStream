# Troubleshooting CerbiStream

This guide lists common issues and resolutions for both developers and operators.

## Build/restore issues
- Newtonsoft.Json missing: Code now uses System.Text.Json. Ensure your app targets .NET8 and restores packages.
- SqlClient errors: The library references System.Data.SqlClient4.9.0. Ensure restore succeeds on your build agents.
- Moq/xUnit missing (tests): The UnitTests project references xUnit, Moq, and Castle.Core. Run `dotnet restore` at the solution root.

## Runtime issues
- Governance not applied
 - Ensure AddCerbiGovernanceRuntime is called during startup
 - Confirm inner factory has providers and is passed to the wrapper
 - Verify profile name and policy path

- Redaction not happening
 - Field might not be listed in DisallowedFields or Forbidden under FieldSeverities
 - Violation codes must be "ForbiddenField" or "DisallowedFieldPresent"

- Policy changes not taking effect
 - GovernanceRuntimeAdapter refreshes its cached fields after an asynchronous file-watcher notification. Rewriting the file does not guarantee that the very next validation uses the new policy.
 - If file watching is unavailable, validation checks the file timestamp at one-second intervals. Ensure the updated file has a later write timestamp.
 - Verify the new rule with a synthetic, non-sensitive payload before relying on a newly tightened policy. Tests should wait for the expected behavior with a bounded timeout and use a separate policy file per test process.

- Relaxed logging mode
 - If `GovernanceRelaxed` is true in the payload, validation/redaction are bypassed

## Diagnostics checklist
- Log out `GovernanceViolations` and `GovernanceProfileVersion` for audit
- Use application-level health endpoints to verify policy is loaded
- Add a startup self-test log that includes a known forbidden field (e.g., `ssn`) in non-prod and assert redaction

## Support data to gather
- Effective profile name and policy path
- Last modified timestamp of the policy file
- A sample log payload before/after governance provider is applied

