# Logging Policy

## Allowed examples

- application startup/shutdown;
- adapter initialized/unavailable;
- operation duration;
- counts of active/recoverable drafts without contents;
- opaque draft IDs;
- safe error category/code;
- retention cleanup counts;
- version/build information.

## Forbidden examples

- draft text or fragments;
- field values;
- clipboard text;
- password/PIN/credential content;
- raw accessibility values that may contain typed text;
- serialized objects containing draft content;
- URL query/form content when it may expose user data;
- exception enrichment that attaches raw draft payloads.

## API design rule

When a content-bearing type is introduced later, avoid `ToString()` implementations that reveal its value. Prefer explicit redacted diagnostics and narrow content access.

## Crash reporting

No external crash/telemetry service is part of the early product. If one is considered later, it requires a separate privacy review and must not receive recoverable draft content.
