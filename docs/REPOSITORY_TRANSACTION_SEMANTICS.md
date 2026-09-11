# Repository Transaction Semantics

**Status:** accepted Phase-4 implementation contract.

## 1. Objective

The repository stores exactly the current protected state of each recoverable draft and must resist stale asynchronous writes, partial mutations, duplicate commands, and crash interruption.

## 2. Persisted monotonic sequence

`ProtectedDraftRecord` includes `SnapshotSequence`.

For a given `DraftId`:

- first persisted snapshot establishes sequence N;
- an incoming sequence `> N` may update the row;
- sequence `== N` is idempotent/no-op if metadata is equivalent;
- sequence `< N` is stale and must never overwrite the row.

This rule is enforced inside the write transaction, not only in the caller.

## 3. Upsert result

Conceptual repository outcome:

```text
ProtectedDraftUpsertResult
- Inserted
- Updated
- IdempotentNoChange
- StaleIgnored
```

No generic boolean return.

## 4. Atomic current-state upsert

A checkpoint is one transaction containing the row mutation required for the current snapshot:

```text
BEGIN write transaction
  validate existing sequence/state
  insert or update same draft_id
  update protected payload + match metadata + timestamps + sequence together
COMMIT
```

The row must never expose new metadata with an old payload or vice versa after a crash.

Do not implement an update as `DELETE` followed by `INSERT` outside a single transaction.

## 5. Delete semantics

`DeleteAsync(DraftId)` is idempotent:

- existing row -> removed;
- missing row -> success/already absent.

Delete reasons are application-layer concepts (`Expired`, `Discarded`, `VerifiedRestored`, `StableClear`, `StrongCompletion`). The repository does not need plaintext or product heuristics to execute the delete.

## 6. Expiry

`DeleteExpiredAsync(now)` performs a metadata-only transaction/query. No payload decryption is allowed.

Listing recoverable drafts always filters `expires_at <= now` even if physical cleanup has not run yet.

## 7. Restore and deletion

Restore write and SQLite delete cannot be one cross-process transaction. Therefore:

1. keep the protected record during target mutation;
2. perform write;
3. verify when supported;
4. only `VerifiedRestored` schedules/deletes the record;
5. if delete then fails, UI may show a non-content diagnostic and retry deletion; it must not inject the draft a second time automatically.

A single-flight restore coordinator prevents duplicate user clicks from causing duplicate writes.

## 8. Discard-all

`DeleteAllRecoverableAsync()` runs as a bounded storage transaction and never decrypts rows. Partial success must not be reported as `AllDiscarded`; the operation returns a typed failure and may be retried.

## 9. Corruption boundary

A malformed row is not silently skipped and reinterpreted. Repository returns a typed corruption/incompatible-record result. Higher layers decide whether the affected record/store is unavailable. No plaintext salvage path exists.

## 10. SQLite baseline

Phase-4 implementation validates:

- rollback journal (`DELETE`) baseline, not WAL;
- `secure_delete=ON` as defense in depth;
- foreign keys enabled if any future dependent tables appear;
- `synchronous=EXTRA` as the durability-first rollback-journal baseline, verified by tests before release;
- bounded busy timeout; no indefinite lock waits;
- one logical writer queue in the process.

Do not rely on connection-library defaults without tests.

## 11. Schema v1 additions

The logical current-state row includes at minimum:

```text
snapshot_sequence
app_profile_version
match_metadata_version
```

alongside the fields already defined in `PERSISTENCE_STORAGE_SPEC.md`.

## 12. Transaction tests

Required:

- stale sequence cannot overwrite new state;
- equal retry is idempotent;
- crash/fault injection before commit leaves old row intact;
- crash/fault injection after commit yields complete new row;
- expiry/delete-all do not call decryptor;
- verified-restore deletion failure does not trigger another automatic target write;
- two queued updates for same DraftId result in the highest committed sequence only.
