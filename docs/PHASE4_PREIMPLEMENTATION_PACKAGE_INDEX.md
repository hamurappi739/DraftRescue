# Phase 4 — Encrypted Persistence Pre-Implementation Package

**Status:** canonical design package. Production persistence is not implemented yet.

## Purpose

Phase 4 turns the single current in-memory draft from Phase 3 into a bounded, local, encrypted, recoverable record. It must not create typing history, plaintext-at-rest, cloud dependencies, or a new path around the SecureInputGuard.

## Mandatory reading order

1. `PERSISTENCE_STORAGE_SPEC.md`
2. `CRYPTOGRAPHIC_ENVELOPE_SPEC.md`
3. `REPOSITORY_TRANSACTION_SEMANTICS.md`
4. `SQLITE_SCHEMA_V1_SPEC.md`
5. `SQLITE_CONNECTION_PRAGMAS_SPEC.md`
6. `PROTECTED_RECORD_MODEL_V1.md`
7. `DPAPI_PAYLOAD_FORMAT_V1.md`
8. `INSTALLATION_SECRET_LIFECYCLE_SPEC.md`
9. `PERSISTENCE_COORDINATOR_SPEC.md`
10. `RETENTION_AND_EXPIRY_EXECUTION_SPEC.md`
11. `STORAGE_CORRUPTION_AND_QUARANTINE_SPEC.md`
12. `MIGRATION_PROTOCOL_V1.md`
13. `PLAINTEXT_AT_REST_CERTIFICATION.md`
14. `SECURE_DELETE_LIMITATIONS.md`
15. `PHASE4_ARCHITECTURE_GUARDS.md`
16. `PHASE4_FAULT_INJECTION_MATRIX.md`
17. `PHASE4_EXIT_CRITERIA.md`

## Canonical data path

```text
FieldTextSnapshot (plaintext, RAM only)
        ↓
DraftPayloadV1 serializer
        ↓
DPAPI CurrentUser protection
        ↓
ProtectedDraftRecordV1
        ↓
serialized writer queue
        ↓
SQLite transaction
        ↓
one current encrypted row per DraftId
```

The repository never accepts `string Text`, `FieldTextSnapshot`, or any plaintext payload type.

## Phase 4 work packages

- **WP-4.1** contracts + schema only.
- **WP-4.2** Windows DPAPI protector + installation secret store.
- **WP-4.3** SQLite bootstrap, pragmas, schema v1.
- **WP-4.4** monotonic encrypted upsert + serialized writer queue.
- **WP-4.5** startup listing + expiry cleanup without decryption.
- **WP-4.6** corruption/quarantine + migration safety.
- **WP-4.7** plaintext-at-rest canary certification + crash/fault tests.
- **WP-4.8** Phase 4 exit gate; stop before Phase 5 UI/decryption workflows.

## Non-goals

Phase 4 does **not** implement Preview, Copy, Restore, browser support, cloud sync, history UI, export, telemetry, or support bundles.
