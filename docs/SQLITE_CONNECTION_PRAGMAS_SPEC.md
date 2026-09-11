# SQLite Connection / PRAGMA Specification

**Status:** Phase-4 validation baseline; final constants require Windows measurement.

## Connection initialization

Every repository connection validates/sets policy explicitly instead of trusting library defaults.

Baseline sequence:

```sql
PRAGMA journal_mode = DELETE;
PRAGMA secure_delete = ON;
PRAGMA foreign_keys = ON;
PRAGMA busy_timeout = 1500;
PRAGMA synchronous = EXTRA;
```

Then validate returned values where SQLite exposes them.

## Why rollback journal / DELETE

DraftRescue intentionally avoids WAL in MVP because WAL can retain previous encrypted page versions until checkpoint/reuse and adds another durable file to the privacy surface. This is not a claim that WAL is unsafe; it is a data-minimization choice for a tiny single-writer desktop workload.

SQLite documents rollback-journal atomic commit and hot-journal recovery. A crash during commit should resolve to old or new complete transactional state, not an application-level half-row.

## Why `synchronous=EXTRA`

SQLite documents `EXTRA` as `FULL` plus directory synchronization after unlinking a rollback journal in DELETE mode, improving durability around power loss. DraftRescue writes are small and infrequent enough that Phase 4 should validate EXTRA first, then downgrade only through a measured ADR if needed.

## Why `secure_delete=ON`

SQLite documents that `secure_delete=ON` overwrites deleted content with zeros inside SQLite-managed database storage. DraftRescue uses it only as defense in depth.

It does **not** justify a product claim of forensic erasure on SSDs, copy-on-write filesystems, snapshots, backups, or storage-controller remapping.

## Busy handling

- bounded `busy_timeout` only;
- no indefinite retry loop;
- one logical writer queue in-process;
- timeout becomes typed storage failure;
- never bypass transaction semantics to "make it work".

## Forbidden silent changes

Runtime code must not silently switch to WAL, MEMORY, OFF journal, `synchronous=OFF`, or `secure_delete=OFF`.

## Official references checked 2026-09-02

- SQLite PRAGMA documentation (`secure_delete`, `synchronous`, `journal_mode`, `busy_timeout`).
- SQLite Atomic Commit / rollback-journal documentation.
