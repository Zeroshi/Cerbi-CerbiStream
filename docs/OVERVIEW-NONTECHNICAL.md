# CerbiStream Overview (Non-Technical)

CerbiStream helps teams log information safely. It automatically hides sensitive information (like Social Security Numbers) and adds tags to each log so teams can see if the log follows company rules.

What it does
- Hides sensitive fields in logs (e.g., SSNs, credit cards)
- Adds helpful tags so dashboards can track policy violations
- Works with your existing logging tools

How it fits in
- Your app logs as usual
- CerbiStream checks the log against your policy
- If sensitive data is found, it replaces it with "***REDACTED***"
- The cleaned log is sent to your usual destinations (console, cloud, SIEM)

What you need
- A small policy file that lists sensitive fields
- A one-line setup change by developers to connect CerbiStream

Benefits
- Reduces risk of personal data leakage
- Helps compliance and audits
- Minimal overhead for developers

Who uses this
- Developers who write apps
- SRE/DevOps teams who manage logging
- Security teams who define the policy file
