# DraftRescue Architecture

## Goal

Create a local Windows recovery layer for temporary unsaved drafts while structurally preventing the product from drifting into keylogger-like behavior.

## Dependency direction

```text
DraftRescue.Domain
        ^
        |
DraftRescue.Application
      ^       ^
      |       |
Infrastructure  Platform.Windows
       \       /
        \     /
     DraftRescue.Desktop

DraftRescue.Tests -> Domain + Application
```

### Domain

Contains stable concepts that do not depend on operating-system APIs, UI frameworks, storage, logging, or networking. Phase 0 deliberately keeps the domain small to avoid prematurely encoding assumptions about how draft capture will work.

### Application

Defines the boundaries between future modules:

- InputObservation
- ContextDetection
- SecureInputGuard
- DraftTracker
- DraftPersistence
- RecoveryMatcher
- RestoreService
- RetentionService
- AppProfiles

The application layer owns privacy-oriented policy contracts such as capture eligibility. It does not know Win32, UI Automation, Avalonia, SQLite, DPAPI, or concrete browser implementations.

### Platform.Windows

The only place for future Windows-specific observation and context adapters, including Win32 and UI Automation. No Windows implementation exists in Phase 0.

This layer must expose normalized metadata through Application contracts rather than leaking raw platform objects upward.

### Infrastructure

The future home for local persistence, encryption-at-rest adapters, repositories, clock/storage implementations, and other non-Windows-specific infrastructure. It must not decide whether a field is safe to capture; that decision belongs to application/security policy before persistence is invoked.

### Desktop

Avalonia UI and the composition root. The UI may invoke Application abstractions but must not contain capture, security classification, persistence, or Win32 logic.

## Privacy boundary

The fundamental flow for future phases is intended to be:

```text
observe supported field/context
        |
        v
normalize minimal metadata
        |
        v
secure/private-context guard  ---- deny/unknown ----> DROP
        |
      allow
        v
track temporary draft
        |
        v
persist locally only through encrypted storage boundary
        |
        v
retention expiry -> delete
```

The secure-input decision must occur before persistence. `Unknown` is not permission.

## Logging boundary

Normal logs may contain structural diagnostics such as module startup, adapter availability, counts, durations, opaque IDs, and error categories.

Normal logs must not contain:

- draft text;
- field values;
- clipboard contents;
- passwords or credentials;
- raw accessibility values that may contain user text;
- browser form contents;
- raw recovery payloads.

When future code needs diagnostics around content processing, log only safe metadata and redacted/opaque identifiers.

## Recovery matching

The canonical product context permits combining executable identity, window-title pattern, accessibility tree information, browser URL where available, UI Automation properties, approximate field geometry, and app-specific adapters. No single identifier should be trusted alone.

Phase 0 does not implement matching and does not decide the final fingerprint format. Future design must minimize raw context storage where an opaque or derived fingerprint can work.

## Composition

Phase 0 uses manual composition rather than introducing a dependency-injection package. This keeps startup explicit and avoids a dependency before the object graph is large enough to justify one. A later ADR may change this if constructor wiring becomes cumbersome.
