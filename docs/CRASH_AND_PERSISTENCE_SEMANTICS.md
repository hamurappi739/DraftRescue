# Crash & Persistence Semantics

**Status:** specification for Phase 4; exact storage engine/encryption envelope deferred to ADR.

## 1. Objective

A recovery product must survive abrupt termination without turning every keystroke into a durable history stream.

## 2. Persist current state, not revisions

For each eligible logical draft session, storage represents the **latest recoverable snapshot** plus minimal metadata.

Updates replace/upsert the current protected payload. Do not append a revision row per edit.

## 3. Coalescing

Writing on every character can cause unnecessary I/O and increase exposure surface.

Use a bounded coalescing strategy:

- update in-memory current state promptly;
- persist after a short idle/debounce interval;
- enforce a maximum dirty age so sustained typing is periodically checkpointed;
- flush best-effort on graceful shutdown/context transition without relying on graceful shutdown for correctness.

Exact timings require measurement during Phase 3/4 and should be constants/configuration owned by the application policy, not scattered magic numbers.

## 4. Atomicity

A persisted record must never expose a half-written plaintext/ciphertext payload.

Storage implementation should support transactional/atomic replacement of:

- encrypted payload;
- payload format version;
- timestamps/expiry;
- minimal matching metadata;
- lifecycle state required for recovery.

## 5. Encryption boundary

Plaintext may exist transiently in process memory only where required for observation, UI preview, copy, restore, and encryption/decryption.

At-rest repository records contain ciphertext. The protection implementation is separate from repository semantics.

Windows DPAPI/`ProtectedData` with CurrentUser scope is a candidate primitive; final choice must be captured in a Phase 4 ADR, including entropy/key-envelope and migration strategy.

## 6. Startup

On startup:

1. open/validate storage;
2. delete expired records before presenting recovery UI;
3. tolerate individually corrupt/un-decryptable records without exposing raw bytes/content;
4. surface only safe diagnostic categories;
5. do not repeatedly retry corrupt payloads forever.

## 7. Corruption

If a record cannot be decrypted/validated:

- never render undeciphered bytes as text;
- mark/drop according to corruption policy;
- log only opaque DraftId/error category;
- do not upload diagnostics/content.

## 8. Delete semantics

Discard, expiry, and successful restore must delete the recoverable record transactionally as soon as practical.

Do not add a hidden recycle/history table.

Physical remnants inside a database/storage medium are a later hardening consideration; storage choice should evaluate secure deletion limitations honestly rather than promising impossible forensic erasure.

## 9. Multi-instance behavior

Default product assumption: one DraftRescue background instance per Windows user session.

Storage should still defend against accidental second instance through an explicit single-instance strategy or safe locking before production persistence is enabled.

## 10. Clock semantics

Store timestamps in UTC.

Expiry must tolerate wall-clock changes. At startup and scheduled cleanup, `ExpiresAt <= UTC now` is expired. Tests must include clock movement and long app downtime.

## 11. Failure injection tests

- terminate during an upsert;
- terminate immediately after typing before debounce;
- terminate after encryption before commit;
- corrupt one record while others remain valid;
- DPAPI/decryption failure;
- storage locked/unavailable;
- disk full/write failure;
- app restart after retention expiration;
- duplicate process/lock contention.
