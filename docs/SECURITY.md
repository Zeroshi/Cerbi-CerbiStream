# Security Considerations

- Treat policy files as sensitive configuration. Control updates via PR reviews.
- Include all PII and regulated fields in policy (DisallowedFields or Forbidden severity).
- Avoid logging secrets; only allow opaque references (IDs) in payloads.
- Use tamper-evident sinks (e.g., append-only storage) for audit trails.
- Ensure host and CI only expose minimal permissions for policy access.
