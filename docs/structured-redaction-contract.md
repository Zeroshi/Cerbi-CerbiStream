# Structured-field redaction and rendered messages

The governance logger applies this contract to structured `IEnumerable<KeyValuePair<string, object>>` states:

- If governance neither redacts nor changes any original field value, the supplied formatter retains its existing behavior.
- After a field changes or is redacted, the original-state formatter is not invoked. The adapter reports actual redacted keys internally, including keys whose final value already equals the replacement marker. This prevents closures over original positional field values from reintroducing those values in the rendered message.
- For a usable `{OriginalFormat}` logging template, the logger renders sanitized values with the Microsoft logging formatter. Alignment, format specifiers, escaped braces, nulls, enumerable values, and repeated positional placeholders are supported. Every occurrence of a redacted field is replaced; repeated unchanged fields retain their positional values.
- For custom states without a usable template, the rendered message is JSON of the sanitized state. Malformed templates and missing template arguments use the same fallback. This intentionally replaces arbitrary custom formatting after redaction. Ordinary rendering failures also use sanitized JSON; if serialization fails (for example, cycles or unsupported values), the message is the fixed non-sensitive diagnostic `[CerbiStream] Sanitized structured event could not be formatted.` Structured context and the original exception still reach the downstream logger, but that final diagnostic does not contain rendered debugging fields.
- The downstream exception object, level, event identity, category and scopes retain their existing handling.
- Detailed scoring, when enabled, receives the same sanitized message for changed structured states. The regression harness uses no external exporter.

This is not universal text sanitization. Exception contents, literal template text, unstructured messages, nested object properties, configured alias values (the existing alias expansion retains them), concurrent mutation of caller-owned state, and custom formatters for unchanged states are outside the tested redaction domain. Do not place sensitive values in those locations and claim they are protected by this change.

Disallowed-field fallback violations are added only when the field exists. Existing violations are retained. An existing `PolicyDisallowedField` or `Forbidden` violation for the same field prevents an equivalent fallback violation; unrelated violations do not suppress it.
