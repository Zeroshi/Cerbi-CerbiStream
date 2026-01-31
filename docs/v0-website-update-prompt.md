# v0 Prompt: Update cerbi.io for CerbiStream v2.0

## Context
CerbiStream v2.0 has been released with major developer-first improvements. The cerbi.io website needs to be updated to reflect these changes, particularly the "How It Works" and "Quickstart" sections.

---

## Prompt for v0

```
Update the cerbi.io website to highlight CerbiStream v2.0's developer-first experience. The key changes are:

### 1. Update the Hero Section Quickstart
Change the "Try CerbiStream in 60 Seconds" call-to-action to emphasize the ONE-LINE setup:

```csharp
// That's it! One line to secure your logs.
builder.Logging.AddCerbiStream();
```

Bullet points should include:
- ✅ PII protection (passwords, SSNs, credit cards auto-redacted)
- ✅ Governance policy auto-generated
- ✅ Console output enabled for development
- ✅ Auto-detects environment variables
- ✅ Ready for production with one method call

### 2. Add New "Configuration Presets" Section
Show the four preset modes in a visual comparison table:

| Preset | Console | Queue | Governance | Telemetry |
|--------|---------|-------|------------|-----------|
| EnableDeveloperMode() | ✅ | ❌ | ✅ | ❌ |
| ForProduction() | ❌ | ✅ | ✅ | ✅ |
| ForTesting() | ✅ | ❌ | ✅ | ❌ |
| ForPerformance() | ❌ | ❌ | ❌ | ❌ |

### 3. Add "Environment Variable Configuration" Section (NEW FEATURE)
This is a major new feature. Create an interactive section showing:

**Zero-code deployments** - Same code works everywhere, controlled by environment:

```bash
# Quick mode switch
export CERBISTREAM_MODE=production
```

Show the key environment variables in a table:
| Variable | Values | Description |
|----------|--------|-------------|
| CERBISTREAM_MODE | development, production, testing, performance | Master preset |
| CERBISTREAM_GOVERNANCE_ENABLED | true/false | Toggle PII redaction |
| CERBISTREAM_QUEUE_ENABLED | true/false | Toggle queue sending |
| CERBISTREAM_CONSOLE_OUTPUT | true/false | Debug in production instantly |

Add Kubernetes/Docker examples showing how the same code deploys everywhere.

### 4. Update "How Cerbi Fits Your Stack" Diagram
Update the diagram to show:
- Environment Variable Detection (auto-configures from env)
- Preset Selection (EnableDeveloperMode, ForProduction, etc.)
- Add the governance auto-generation step

### 5. Add "What's New in v2.0" Badge/Banner
At the top of the page, add a notification banner:
"🚀 CerbiStream v2.0 Released - One-line setup, environment variable configuration, and preset modes"

### 6. Update Test Coverage Stats
Change the stats to show:
- 325 tests passing
- 55 integration tests
- 270 unit tests

### 7. Add "Debug Production Issues Instantly" Section
Show how environment variables enable instant debugging:

```bash
# Enable console output without redeploying
kubectl set env deployment/myapp CERBISTREAM_CONSOLE_OUTPUT=true

# Disable queue temporarily
kubectl set env deployment/myapp CERBISTREAM_QUEUE_ENABLED=false
```

### 8. Visual Style
- Use the existing cerbi.io design system
- Green checkmarks for features
- Code blocks with syntax highlighting
- Interactive toggles for the environment variable demo if possible
- Mobile-responsive tables

### 9. Key Messages to Emphasize
1. "One line of code" - Simplicity is the hero
2. "Zero-code configuration changes" - Environment variables are powerful
3. "Developer-first" - Not just secure, but easy
4. "Works with your existing stack" - Complements, doesn't replace
5. "325 tests passing" - Quality and reliability

### 10. SEO Keywords to Include
- Developer-first logging governance
- One-line .NET logging setup
- Environment variable configuration for logging
- PII redaction .NET
- Kubernetes logging governance
- Zero-config logging setup
```

---

## Summary of CerbiStream v2.0 Features

### Developer-First Experience
- **One-line setup**: `builder.Logging.AddCerbiStream()` just works
- **Auto-generated governance**: Creates `cerbi_governance.json` with sensible PII defaults
- **Preset modes**: EnableDeveloperMode(), ForProduction(), ForTesting(), ForPerformance()

### Environment Variable Configuration (NEW!)
- **CERBISTREAM_MODE**: Master switch (development/production/testing/performance)
- **CERBISTREAM_GOVERNANCE_ENABLED**: Toggle PII redaction on/off
- **CERBISTREAM_QUEUE_ENABLED**: Toggle queue sending
- **CERBISTREAM_CONSOLE_OUTPUT**: Debug production instantly
- **20+ environment variables** for complete control

### Enterprise Features
- Azure App Insights integration
- Queue scoring for analytics
- AES-256 encrypted file fallback
- Hot-reload governance policies

### Test Coverage
- 325 tests passing
- 55 integration tests
- 270 unit tests (135 × 2 frameworks)
