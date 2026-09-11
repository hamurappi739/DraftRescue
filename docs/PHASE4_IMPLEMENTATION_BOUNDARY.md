# Phase 4 Implementation Boundary

## Allowed

Phase 4 may introduce only the infrastructure required to persist already-authorized current draft snapshots:

- protected payload serialization;
- Windows DPAPI `CurrentUser` adapter;
- installation HMAC secret lifecycle;
- SQLite repository and schema bootstrap;
- serialized writer queue;
- monotonic `SnapshotSequence` upsert;
- metadata-only recoverable listing;
- retention/expiry cleanup;
- migration/corruption handling;
- privacy-at-rest certification tests.

## Forbidden

Do not implement:

- target-content acquisition changes;
- security classifier changes to make more targets Allowed;
- Preview/Copy/Restore;
- plaintext snippets in DB;
- revision/history tables;
- WAL mode without a new privacy ADR;
- cloud/network persistence;
- backup/export of draft bodies;
- automatic database upload for diagnostics;
- LocalMachine DPAPI scope;
- unencrypted fallback;
- application master encryption key unless a future ADR replaces direct DPAPI.

## Dependency boundary

```text
Application orchestration
   ├── IDraftProtector
   ├── IProtectedDraftRepository
   └── ICheckpointCoordinator

Infrastructure
   └── SQLite implementation

Platform.Windows
   ├── DPAPI implementation
   └── per-user storage-path resolution
```

`DraftRescue.Infrastructure` must not reference UI Automation. `DraftRescue.Platform.Windows` cryptography code must not know SQLite schema details.
