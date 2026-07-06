# CerbiStream Quick Start

Get secure, governed logging in **one line of code**.

## 🚀 Installation

```bash
dotnet add package CerbiStream
```

## ⚡ Setup (One Line!)

```csharp
// Program.cs
builder.Logging.AddCerbiStream();
```

**That's it!** You now have:
- ✅ PII protection (passwords, SSNs, credit cards auto-redacted)
- ✅ Governance policy auto-generated
- ✅ Console output for development
- ✅ Ready for production upgrade
- ✅ **Auto-detects environment variables** for zero-code config changes!

## 📝 Usage

```csharp
// Just use standard ILogger - CerbiStream handles the rest
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger) => _logger = logger;
    
    public void ProcessUser(string userId, string email)
    {
        // PII fields are automatically redacted based on governance policy
        _logger.LogInformation("Processing {userId} with {email}", userId, email);
    }
}
```

## 🎯 Common Scenarios

### Development (Default)
```csharp
builder.Logging.AddCerbiStream(); // Uses EnableDeveloperMode() automatically
```

### Production
```csharp
builder.Logging.AddCerbiStream(o => o.ForProduction());
```

### Testing
```csharp
builder.Logging.AddCerbiStream(o => o.ForTesting());
```

### High Performance
```csharp
builder.Logging.AddCerbiStream(o => o.ForPerformance());
```

---

## 🌍 Environment Variable Configuration (NEW!)

**Zero code changes** to switch between environments! Just set environment variables.

### Quick Mode Switch

```bash
# Linux/Mac
export CERBISTREAM_MODE=production

# Windows PowerShell
$env:CERBISTREAM_MODE = "production"

# Docker
docker run -e CERBISTREAM_MODE=production myapp

# Kubernetes
env:
  - name: CERBISTREAM_MODE
    value: "production"
```

**Available modes:** `development`, `production`, `testing`, `performance`

### All Environment Variables

| Variable | Values | Description |
|----------|--------|-------------|
| `CERBISTREAM_MODE` | `development`, `production`, `testing`, `performance` | Master mode switch |
| `CERBISTREAM_GOVERNANCE_ENABLED` | `true`/`false` | Toggle PII redaction |
| `CERBISTREAM_GOVERNANCE_PROFILE` | Profile name | e.g., `myapp`, `default` |
| `CERBI_GOVERNANCE_PATH` | File path | Path to governance JSON |
| `CERBISTREAM_QUEUE_ENABLED` | `true`/`false` | Toggle queue sending |
| `CERBISTREAM_QUEUE_TYPE` | `AzureServiceBus`, `RabbitMQ`, etc. | Queue provider |
| `CERBISTREAM_QUEUE_CONNECTION` | Connection string | Queue connection |
| `CERBISTREAM_QUEUE_NAME` | Queue name | Target queue/topic |
| `CERBISTREAM_ENCRYPTION_MODE` | `None`, `Base64`, `AES` | Encryption type |
| `CERBISTREAM_CONSOLE_OUTPUT` | `true`/`false` | Console logging |
| `CERBISTREAM_TELEMETRY_ENABLED` | `true`/`false` | Telemetry sending |

### Layered Configuration

Environment variables + code config work together:

```csharp
// Start from environment, then override specific settings
builder.Logging.AddCerbiStream(o => o
    .FromEnvironment()                    // Load from env vars
    .WithGovernanceProfile("override"));  // Code takes precedence
```

### Example: Production Kubernetes Deployment

```yaml
# deployment.yaml
env:
  - name: CERBISTREAM_MODE
    value: "production"
  - name: CERBISTREAM_QUEUE_ENABLED
    value: "true"
  - name: CERBISTREAM_QUEUE_TYPE
    value: "AzureServiceBus"
  - name: CERBISTREAM_QUEUE_CONNECTION
    valueFrom:
      secretKeyRef:
        name: cerbistream-secrets
        key: queue-connection
  - name: CERBISTREAM_QUEUE_NAME
    value: "logs-queue"
```

```csharp
// Program.cs - same code for all environments!
builder.Logging.AddCerbiStream();
```

### Debug Production Issues Instantly

```bash
# Enable console output in production without redeploying
kubectl set env deployment/myapp CERBISTREAM_CONSOLE_OUTPUT=true

# Disable queue temporarily for debugging
kubectl set env deployment/myapp CERBISTREAM_QUEUE_ENABLED=false
```

---

## 🔧 Custom Configuration

```csharp
builder.Logging.AddCerbiStream(options => options
    .ForProduction()
    .WithGovernanceProfile("myapp")
    .WithQueueRetries(true, retryCount: 5)
    .WithAesEncryption());
```

## 📁 Governance Policy (Auto-Generated)

CerbiStream automatically creates `cerbi_governance.json` with sensible defaults:

```json
{
  "Version": "1.0",
  "LoggingProfiles": {
    "default": {
      "DisallowedFields": ["password", "ssn", "creditCard", "secret", "token", "apiKey"],
      "FieldSeverities": {}
    }
  }
}
```

Customize this file to add your own rules. Changes are hot-reloaded automatically.

## 🔗 Next Steps

- [Full Installation Guide](INSTALLATION.md)
- [Production Checklist](README-PRODUCTION.md)
- [Troubleshooting](TROUBLESHOOTING.md)
- [Technical Walkthrough](WALKTHROUGH-TECHNICAL.md)

---

**Questions?** Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md) or open an issue.

## Governance evidence metadata

When CerbiStream runtime governance is enabled, governed scoring events now carry lightweight evidence metadata alongside existing violation and governance mode fields. The runtime stamps these fields locally after policy validation and redaction; it does not call the control plane or perform remote governance lookup on the logging hot path.

Emitted evidence fields include:

- `GovernanceProfileId` when the active profile id/name can be resolved from `cerbi_governance.json`.
- `GovernanceProfileVersion` when the active profile or root governance config declares a version.
- `GovernanceProfileHash`, a stable SHA-256 hash of the canonical active profile JSON. If no active profile can be extracted, CerbiStream hashes the full governance config JSON instead.
- `GovernanceDecision`:
  - `allowed` when no violations are present and the event is not relaxed.
  - `warned` when violations are present but no redaction or blocking action occurred.
  - `redacted` when one or more policy-governed fields were redacted.
  - `relaxed` when `GovernanceRelaxed` is true.
  - `blocked` is reserved for an existing block mode and is not introduced by this runtime update.
- `EnforcementAction`:
  - `none` for allowed events.
  - `warn` for warned events.
  - `redact` for redacted events.
  - `allow` for relaxed events, documenting that relaxed mode bypassed enforcement without dropping application flow.

If the governance config is missing or malformed, CerbiStream keeps emitting the scoring event and omits evidence fields it cannot compute. No new required configuration fields are introduced.
