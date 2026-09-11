# Persistence Storage Specification

**Status:** accepted MVP baseline, subject only to implementation validation in Phase 4.

## 1. Storage technology

Use a small local SQLite database behind `IDraftRepository` for recoverable-record metadata and protected payload bytes.

Rationale:

- atomic transactions;
- bounded current-state records;
- simple retention queries;
- predictable crash recovery;
- no need to invent a custom index/file journal;
- repository abstraction prevents SQLite concerns from leaking into domain/application layers.

## 2. Privacy constraints

SQLite is never allowed to receive plaintext draft text.

The application must construct `ProtectedDraftRecord` before the repository call. Repository APIs accept protected payload bytes only.

Do not persist:

- plaintext draft;
- plaintext preview/snippet;
- full raw URL;
- raw page title by default;
- field label/name unless explicitly transformed into an approved fingerprint;
- clipboard contents;
- keystroke/edit history.

## 3. Database location

Per-user local application data directory, under a DraftRescue-owned directory. Do not use roaming storage or cloud-synchronized locations by default.

Exact Windows path is platform-layer responsibility. Tests use isolated temporary directories.

## 4. SQLite journal policy

MVP preference:

- do **not** enable WAL;
- use a rollback journal mode suitable for bounded desktop writes (`DELETE` baseline);
- enable `secure_delete=ON` as defense-in-depth for deleted database content;
- do not claim this provides forensic secure erasure on SSDs/filesystems.

Reason: WAL naturally keeps older page versions around until checkpointing, which conflicts with DraftRescue's data-minimization goal even when the draft body is encrypted.

If performance testing later demonstrates a need for WAL, an ADR must explicitly evaluate privacy/retention implications before changing this. Connection policy is further fixed by `SQLITE_CONNECTION_PRAGMAS_SPEC.md`, with `synchronous=EXTRA` as the durability-first Phase-4 baseline.

## 5. Schema v1

The normative schema is now `SQLITE_SCHEMA_V1_SPEC.md`. It includes persisted `snapshot_sequence`, profile/match metadata versions, presentation kind, and explicit row constraints. There is exactly one current row per `DraftId`; there is no revision/history table.

## 6. Upsert semantics

Updating a draft:

1. receive already-protected new payload;
2. begin transaction;
3. update the same `draft_id` row;
4. update `updated_at` and expiry;
5. commit;
6. discard in-memory prior plaintext/protected transient buffers as soon as practical.

Never insert a new row merely because text changed.

## 7. Durability checkpoints

Writes are controlled by DraftTracker/PersistenceCoordinator policy, not every keystroke. Exact debounce timing remains Phase-3/4 performance tuning, but implementation must enforce:

- debounce frequent changes;
- maximum dirty age so a long typing session is periodically checkpointed;
- immediate/expedited checkpoint on meaningful context-loss/shutdown paths when still eligible;
- cancellation does not turn into plaintext fallback.

## 8. Startup recovery

On startup:

1. open database;
2. validate schema/user-version;
3. delete expired rows before listing recoverable items;
4. do not decrypt all payloads during startup;
5. surface metadata-only recoverable items;
6. decrypt only for explicit preview/copy/restore/application need.

## 9. Corruption behavior

Database corruption must not trigger exports/dumps containing protected or plaintext payloads.

MVP policy:

- fail closed;
- preserve the corrupt database file only if needed for local diagnostic recovery and without uploading it;
- UI may state that recoverable drafts are unavailable;
- do not silently rebuild and claim recovery succeeded;
- a future support/export workflow requires a separate privacy review.

## 10. Deletion semantics

Logical product deletion means the record becomes inaccessible to DraftRescue and is removed from normal database state.

Use transaction delete + `secure_delete=ON` as defense-in-depth. Do not promise anti-forensic physical erasure from flash media.

Deletion triggers:

- expiry;
- explicit Discard;
- successful Restore according to restore policy;
- stable manual clear/completion when rules establish draft no longer recoverable;
- explicit "Discard all recoverable drafts";
- incompatible/reset cryptographic identity where safe migration is impossible.

## 11. Migration policy

- SQLite `user_version` / explicit schema version;
- migrations are forward-only inside a release;
- migration code never decrypts payload merely to migrate unrelated metadata;
- any migration that needs plaintext requires a dedicated security ADR and tests;
- migration failure leaves original data untouched where technically possible and fails closed.

## 12. Tests

- one draft remains one row after hundreds of updates;
- no plaintext fixture substring appears in raw database bytes;
- expiry query excludes expired records;
- delete removes logical record;
- crash between transaction begin/commit leaves either old or new complete record, never malformed partial record;
- repository cannot accept `string plaintext` through its public API;
- startup listing does not call decryptor.
