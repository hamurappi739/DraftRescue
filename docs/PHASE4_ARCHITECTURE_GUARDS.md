# Phase 4 Architecture Guards

## Compile-time / dependency rules

1. Repository public API accepts `ProtectedDraftRecordV1`, never `string`, `FieldTextSnapshot`, or `DraftPlaintext`.
2. SQLite implementation cannot reference Avalonia/UI projects.
3. DPAPI adapter cannot reference SQLite provider types.
4. Recoverable-list query DTO has no protected payload/body member.
5. Logging abstractions are not passed plaintext payload types.
6. No HTTP/network client dependency in persistence path.

## Source guards

Phase-4 review should reject:

- `journal_mode=WAL` without approved ADR;
- `synchronous=OFF`;
- DPAPI `LocalMachine`;
- fallback file containing plaintext;
- SQL column names such as `plaintext`, `snippet`, `preview_text`, `raw_url`, `window_title`;
- repository overloads accepting user text;
- `SELECT *` in recovery list path;
- auto-recreate-on-corruption that deletes old store without explicit policy.

## Behavioral guards

- protect failure ⇒ repository invocation count 0;
- list recoverables ⇒ unprotect invocation count 0;
- expiry cleanup ⇒ unprotect invocation count 0;
- stale write ⇒ stored sequence and ciphertext unchanged;
- DB busy/locked ⇒ bounded typed failure, no unencrypted side channel.
