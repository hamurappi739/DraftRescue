# Configuration Specification

## 1. Philosophy

DraftRescue should work with safe defaults and expose only settings users can understand. Security-critical internals are not ordinary toggles.

## 2. MVP user settings

### Retention duration

Values from `RETENTION_POLICY_SPEC.md`.

### Launch behavior

`Start DraftRescue when I sign in` — visible On/Off setting. Recommended default after normal installation/first launch: On. See `SYSTEM_LIFECYCLE_UX_SPEC.md`.

### App support visibility

A future settings page may show which app profiles are supported/enabled, but enabling a profile must not bypass global privacy rules.

## 3. Not user-configurable in MVP

Do not expose toggles such as:

- `Capture password fields`;
- `Allow private browsing`;
- `Ignore secure input guard`;
- `Force restore when match is uncertain`;
- `Keep history forever`;
- `Log draft contents for debugging`.

These contradict product invariants.

## 4. Storage

Settings may be stored separately from encrypted draft payloads because most settings are non-secret. Still avoid storing unnecessary user/context data in configuration.

## 5. Defaults

Initial proposed defaults:

```text
Retention: 30 minutes
Private browsing: always excluded
Secure fields: always excluded
Telemetry: none
Cloud sync: none
Auto-restore: off / not available
```

Only retention is a normal user preference in the initial scope.

## 6. Validation

Configuration loading must validate ranges/enums. Invalid or corrupt configuration falls back to safe documented defaults rather than disabling privacy controls.

## 7. Migration

Settings schema should have a small version number when persistence is introduced. Migration must never reinterpret a missing privacy field as permissive behavior.
