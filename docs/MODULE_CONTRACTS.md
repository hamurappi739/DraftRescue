# Module Contracts — Design Map

**Status:** architecture preparation, not runtime implementation.

## InputObservation

**Purpose:** notice supported editable-field activity/context changes.

**Must not:** expose a long-term/global keystroke history.

**Output direction:** normalized candidate context/events with minimum necessary metadata.

## ContextDetection

**Purpose:** determine active application/window/field context using platform information.

**Must not:** make persistence decisions itself.

## SecureInputGuard

**Purpose:** decide whether the current context is eligible for draft handling.

**Invariant:** uncertainty is denial, not permission.

## DraftTracker

**Purpose:** maintain the current temporary draft state for explicitly eligible fields.

**Must not:** become a historical archive of everything typed.

**Accepted semantic:** maintain the latest current draft state, not durable revision history. Exact field snapshot acquisition mechanism remains profile/control-specific and must be validated experimentally.

## DraftPersistence

**Purpose:** persist only protected recoverable drafts locally with timestamps/expiry.

**Preferred boundary:** repository storage should receive protected/encrypted payloads rather than encouraging arbitrary plaintext writes.

**Accepted baseline:** SQLite current-state repository + DPAPI CurrentUser protected payload v1 behind abstractions. See `PERSISTENCE_STORAGE_SPEC.md`, `REPOSITORY_TRANSACTION_SEMANTICS.md`, and `CRYPTOGRAPHIC_ENVELOPE_SPEC.md`.

## RecoveryMatcher

**Purpose:** match a recoverable draft back to the intended application/window/field using multiple signals.

**Invariant:** do not trust one identifier alone.

## RestoreService

**Purpose:** safely return the chosen draft to the intended target or provide a safer fallback.

**Invariant:** ambiguous target must not receive blind restore.

## RetentionService

**Purpose:** expire/delete drafts at configured retention boundaries.

**Invariant:** expiry is enforced automatically, including cleanup after restart.

## AppProfiles

**Purpose:** isolate application-specific detection/recovery knowledge from the core engine.

**Invariant:** app-specific exceptions must not weaken global secure-input policy.

## UI

**Purpose:** nearly invisible normal operation; when recovery is available expose Restore, Preview, Copy, Discard.

**Must not:** contain platform capture, encryption, or persistence logic.


## Application use cases

The application-layer command/query boundary is normative in `USE_CASE_CONTRACTS_V1.md`. UI invokes those use cases rather than module implementations directly. Placement is defined in `CONTRACT_PROJECT_OWNERSHIP_MATRIX.md`.
