# ADR 0057 — SQLite DELETE journal + synchronous EXTRA baseline

**Decision:** Phase 4 validates `journal_mode=DELETE`, `synchronous=EXTRA`, `secure_delete=ON`, bounded busy timeout and one logical writer.

**Reason:** small desktop workload favors simple rollback-journal durability and minimized durable side files. SQLite documents EXTRA as adding directory sync after rollback-journal unlink in DELETE mode.
