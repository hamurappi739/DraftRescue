# Observability and Metrics Specification

## 1. Default stance

DraftRescue is local-first and does not require remote telemetry for MVP. Diagnostics are local structural logs only.

## 2. Safe structural events

Examples:

- app started/stopped;
- profile matched / unsupported;
- field classification category (Allowed/Denied reason) without field text/name;
- snapshot read success/failure category, optionally length bucket rather than exact content;
- persistence success/failure;
- cleanup count;
- recovery match kind;
- restore success/failure category;
- provider timeout/backoff.

## 3. Forbidden fields

Never log:

- draft text;
- plaintext preview;
- clipboard text;
- full URL;
- page/window title by default;
- accessibility name/label when user-derived;
- password/PIN/security code/card number;
- protected payload bytes;
- DPAPI blob;
- fingerprint secret key;
- raw HMAC source strings.

## 4. Length/privacy

Even exact text length can occasionally be sensitive. Prefer coarse buckets where useful:

```text
empty, 1-15, 16-63, 64-255, 256-1023, 1024+
```

Do not log per-character update events.

## 5. Local log retention

Keep diagnostic retention short and configurable only if needed. Logs are not a user activity history. Rotation and max total size are mandatory before production.

## 6. Future telemetry

Any remote telemetry is out of MVP and requires explicit product/privacy review. If ever added, it must be content-free, opt-policy-defined, and structurally impossible to serialize draft plaintext.
