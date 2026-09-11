# ADR 0059 — One serialized persistence writer

**Decision:** database mutations pass through one logical in-process writer queue.

**Reason:** simplify locking, ordering, stale-sequence reasoning and bounded busy behavior. Repository transaction sequence checks remain the final authority.
