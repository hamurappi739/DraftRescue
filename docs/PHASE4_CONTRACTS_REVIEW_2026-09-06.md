# Phase 4 WP4.1 Contracts Review — 2026-09-06

## Decision

WP4.1 (protected-record contracts and canonical SQLite schema declaration) is **Pass**. The work remains deliberately below the runtime persistence boundary: no SQLite connection, DPAPI call, installation secret, coordinator, decryption, preview, copy, or restore was added.

## Implemented

- `ProtectedDraftRecordV1` is the only record shape accepted by `IProtectedDraftRepository`.
- The repository surface accepts protected records, opaque `DraftId` values, and metadata-only list results; it has no plaintext, snapshot, decrypt, or content-bearing overload.
- `DraftPlaintextPayload` and `ProtectedDraftPayload` are separated at the protector boundary. Protected bytes are copied on ingress and egress.
- Record construction validates non-empty identity/metadata, positive format versions, non-negative `SnapshotSequence`, and monotonic timestamps.
- `SqliteSchemaV1` declares the exact v1 `drafts` table, expiry index, `WITHOUT ROWID`, monotonic timestamp/sequence checks, and safe connection baseline (`DELETE`, `secure_delete=ON`, `foreign_keys=ON`, `busy_timeout=1500`, `synchronous=EXTRA`). WAL and body-bearing presentation columns are absent.

## Evidence

- `dotnet build tests\DraftRescue.Tests\DraftRescue.Tests.csproj --no-restore -c Debug`: 0 warnings, 0 errors.
- `dotnet test tests\DraftRescue.Tests\DraftRescue.Tests.csproj --no-build -c Debug`: **121/121**.
- `scripts/phase4_contracts_guard.ps1`: **Pass**, zero forbidden repository/schema violations.
- `scripts/run_phase4_contracts_gate.ps1 -SkipBuild`: **Pass**; artifact `artifacts/phase4-contracts-gate/PHASE4-CONTRACTS-GATE.json`.

## Privacy and correctness invariants

Relevant controls: P-002, P-003, P-004, P-005, P-006, P-017, P-025, P-028, P-030, P-031, P-032, P-033, P-035, P-036, P-037; C-019, C-020, C-026, C-027, C-028, C-029, C-030, C-031, C-032, C-033.

## Next bounded work package

WP4.2: implement and test DPAPI CurrentUser payload format v1 and installation-secret lifecycle. It must preserve the contracts above and still stop before SQLite runtime/coordinator work.
