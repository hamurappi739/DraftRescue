# Encrypted Checkpoint Commit State Machine

## Objective

Make checkpoint behavior explicit under coalescing, protection latency, storage contention, stale completions, cancellation, and crashes.

## Per-Draft logical states

```text
Clean
  │ new current snapshot
  ▼
Dirty
  │ scheduler selects newest sequence
  ▼
Protecting(sequence N)
  ├── newer snapshot arrives ──→ Dirty(newer pending) while N may finish
  ├── protect fails ───────────→ Dirty + bounded retry policy
  ▼
ProtectedPendingWrite(N)
  │ serialized writer queue
  ▼
Writing(N)
  ├── StaleIgnored ────────────→ use newer committed/pending state
  ├── store failure ───────────→ Dirty + bounded retry
  └── commit success ──────────→ Committed(N)
                                  │
                                  ├── newer pending → Dirty
                                  └── no newer → Clean
```

## Rules

1. `CommittedSequence` only advances after repository commit success.
2. A successful DPAPI operation does not mean the draft is durable.
3. `StaleIgnored` never decrements committed state.
4. Do not retain old plaintext snapshots merely because an older protection/write is still running.
5. Cancellation can discard ciphertext that has not been committed; it never writes plaintext as compensation.
6. On process crash, SQLite transaction state is the durable authority; in-memory scheduler state is disposable.

## Shutdown

Do not start a new UI Automation read during Windows shutdown. If an already-current authorized in-memory snapshot is dirty and time permits, the coordinator may request a bounded checkpoint. Recovery correctness must not depend on that final attempt.
