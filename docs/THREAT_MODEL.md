# DraftRescue Threat Model — Initial

## Assets

- temporary unsaved draft content;
- metadata identifying application/window/field context;
- future encryption keys or protected key material;
- recovery state and retention timestamps.

## Trust boundaries

1. External application/browser -> Windows observation adapter.
2. Windows platform adapter -> Application layer.
3. Application layer -> local persistence boundary.
4. Local persistence -> OS filesystem/account boundary.
5. Recovery UI -> restore/copy operation.

## Primary threats

### T1 — Keylogger drift

A convenient global input stream becomes a de facto history of everything typed.

**Mitigation direction:** supported-field observation, minimal state, no global history, explicit secure/private guard, strict retention.

### T2 — Secure-field false negative

A password or credential control is misclassified as ordinary text.

**Mitigation direction:** fail closed; combine signals; test known secure controls; never treat uncertainty as permission.

### T3 — Private-browsing capture

Incognito/InPrivate/Private content becomes recoverable.

**Mitigation direction:** private modes excluded by default; app-specific detection; uncertain browser context denies persistence.

### T4 — Plaintext at rest

Recoverable drafts are written unencrypted.

**Mitigation direction:** encrypted persistence phase; Windows protection such as DPAPI may protect key material/data; no persistence before the encryption boundary is implemented and reviewed.

### T5 — Logging leakage

Raw draft/UI Automation/clipboard values enter logs or crash reports.

**Mitigation direction:** prohibit content logging; structural diagnostics only; review exception paths.

### T6 — Wrong-field restore

Recovered text is restored into a different field/window than intended.

**Mitigation direction:** multi-signal recovery matching; do not rely on one identifier; safe fallback to Preview/Copy rather than blind restore when confidence is insufficient.

### T7 — Excessive retention

Sensitive drafts remain recoverable longer than configured.

**Mitigation direction:** explicit expiry timestamps; retention service; startup cleanup; tests around expiry boundaries.

### T8 — Clipboard leakage

A future Copy/Restore implementation leaves plaintext in the system clipboard.

**Mitigation direction:** treat clipboard as an explicit risk; prefer direct safe restore where supported; define clipboard lifecycle before implementation.

### T9 — Resource abuse

Continuous UI inspection causes excessive CPU usage or responsiveness problems.

**Mitigation direction:** event-driven observation where safe/available, bounded polling where necessary, backoff, profiling, adapter-specific limits.

## Explicitly out of scope in Phase 0

Phase 0 contains no observation, persistence, encryption, restore, clipboard, browser integration, system tray, startup registration, telemetry, analytics, or AI.
