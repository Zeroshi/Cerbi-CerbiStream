
### CerbiShield Scoring Integration

CerbiStream integrates with **CerbiShield** for centralized governance scoring and analytics.

**Automatic Scoring Enablement:**
- If your `cerbi_governance.json` contains a `tenantId` field, scoring is **automatically enabled**
- No additional code changes required - just add your tenant ID to the config

```json
{
  "tenantId": "your-tenant-id",
  "Version": "1.0",
  "LoggingProfiles": {
    "default": {
      "DisallowedFields": ["password", "ssn", "creditCard"]
    }
  }
}
```

**Manual Configuration:**
```csharp
builder.Logging.AddCerbiStream(options => options
    .ForProduction()
    .WithQueue("AzureServiceBus", connectionString, "cerbishield.log-scoring")
);
```

**What gets sent to CerbiShield:**
- Governance violations and severities
- Application identity (app name, version, instance ID)
- Environment and deployment information
- Log metadata for compliance analytics

**Note:** Scoring requires a configured Azure Service Bus queue.
