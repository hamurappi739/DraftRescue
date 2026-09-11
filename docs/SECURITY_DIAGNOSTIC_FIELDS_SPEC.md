# Security Diagnostic Fields Specification

## Safe default fields

Security diagnostics may include only bounded structural data such as:

```text
error_code
operation
component
profile_id              product-owned static id only
profile_revision
app_version_bucket
control_type_enum
framework_id_enum/bucket when audited
generation_delta_bucket
provider_health_enum
private_mode_enum
protected_signal_status
sensitive_purpose_enum
positive_allow_state
capability_state_enum
duration_bucket_ms
```

## Forbidden fields

Never include:

- draft/field text;
- UIA Name/HelpText/ItemStatus;
- raw AutomationId/ClassName unless an explicit logging audit approves a static fixture/product-owned value;
- raw window/page/document title;
- URL/origin/path;
- capability nonce/binding id;
- provider exception message/stack locals containing provider data;
- clipboard text.

## Denial diagnostics

Normal privacy denials are expected control flow and should not create scary user notifications or verbose exception logs. Use stable counts/codes where diagnostics are enabled.
