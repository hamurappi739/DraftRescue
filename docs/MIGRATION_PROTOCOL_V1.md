# Persistence Migration Protocol v1

## Principle

Schema/protection migrations must preserve fail-closed behavior and must not decrypt draft bodies unless the migration explicitly requires a protection-format change approved by a security ADR.

## Startup order

```text
open store
  ↓
read PRAGMA user_version
  ↓
version == supported? ─ yes → normal startup
  ↓ no
older + known migration path? ─ no → IncompatibleStore
  ↓ yes
exclusive migration transaction
  ↓
validate postconditions
  ↓
commit + set user_version
```

## Migration properties

- forward-only per installed release;
- transactional where SQLite permits;
- idempotent detection after interrupted startup;
- original database not intentionally destroyed before a successful commit;
- no body decryption for adding/indexing metadata columns;
- no silent downgrade.

## Protection-version migration

Not part of schema-v1 MVP. If v2 protection is introduced, migration must specify whether ciphertext is lazily reprotected on explicit use or eagerly decrypted/reprotected. Either choice requires plaintext-lifetime tests and a separate ADR.

## Failure

Migration failure disables affected recovery storage for that launch and emits a stable content-free code. It must not `DROP` and recreate the store automatically while recoverable drafts may exist.
