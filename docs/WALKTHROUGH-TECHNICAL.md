# Technical Walkthrough

This walkthrough explains how CerbiStream governance runtime integrates with the .NET logging pipeline and how validation/redaction occurs.

## High-level flow
1. Application logs via ILogger.LogInformation("{ssn} {userId}", ...)
2. Cerbi GovernanceLoggerProvider intercepts the log state
3. GovernanceRuntimeAdapter validates the state and collects fields to redact
4. Forbidden fields are redacted in-place ("***REDACTED***")
5. The modified state is forwarded to the inner ILoggerFactory providers

## Key classes
- GovernanceRuntimeAdapter
 - ValidateAndRedactInPlace(IDictionary<string, object>)
 - Loads DisallowedFields and Forbidden fields from the policy file
 - Parses GovernanceViolations[] and emits redactions for forbidden fields
- GovernanceLoggerProvider
 - Wraps the pipeline to invoke the adapter on structured logs
- LoggingBuilderExtensions.AddCerbiGovernanceRuntime
 - Simplifies registration, allowing you to pass an inner factory with sinks

## Policy file structure
{
 "Version": "1.0.0",
 "LoggingProfiles": {
 "default": {
 "DisallowedFields": ["ssn"],
 "FieldSeverities": {
 "creditCard": "Forbidden"
 }
 }
 }
}

- DisallowedFields: always redacted
- FieldSeverities: any field set to "Forbidden" is redacted

## Relaxation
If the payload contains `GovernanceRelaxed: true`, governance checks and redactions are skipped. Use this intentionally, e.g., for test logs.

## Adapter internals
- Caches parsed policy fields and reloads when file write time increases
- Handles violations passed as objects, JsonElements, or JSON strings
- Uses case-insensitive key matching for field names

## Extending sources
`FileGovernanceSource` is the default runtime source. You can implement `IRuntimeGovernanceSource` to pull policies from cloud storage or other sources and pass it to the validator.

## Sample unit test
```
var sink = new TestSink();
var inner = LoggerFactory.Create(b => b.AddProvider(sink));
var adapter = new GovernanceRuntimeAdapter("default", tmpPolicyPath);
var provider = new GovernanceLoggerProvider(inner, adapter);
var logger = LoggerFactory.Create(b => b.AddProvider(provider)).CreateLogger("test");

logger.LogInformation("{ssn} {userId}", "111-22-3333", "u1");

Assert.Equal("***REDACTED***", sink.Events[0].GetValue("ssn"));
```

## Performance considerations
- Redaction uses in-place mutation on a Dictionary for minimal allocations
- Policy is cached to avoid repeated disk reads
- Adapter is safe for high-throughput structured logs
