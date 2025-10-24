# Operations Checklist (Production)

Use this checklist when promoting to production or performing maintenance.

Pre-deploy
- [ ] Governance policy reviewed and approved
- [ ] Profile names finalized (e.g., "default", per-domain)
- [ ] CERBI_GOVERNANCE_PATH set per environment
- [ ] Logging sinks configured (AppInsights/Console/Storage/Queues)
- [ ] Fallback logging enabled for outages

Post-deploy
- [ ] Sample log verified: expected redactions (e.g., ssn)
- [ ] GovernanceViolations metric visible in dashboards
- [ ] Policy file accessibility verified and timestamp captured

Ongoing
- [ ] Regular policy audits with Security/Compliance
- [ ] Rotate encryption keys if applicable
- [ ] Alerting on repeated violations
