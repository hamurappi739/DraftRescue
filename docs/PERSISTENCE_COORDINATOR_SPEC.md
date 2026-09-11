# Persistence Coordinator Specification

## Responsibility

Bridge the current Phase-3 in-memory draft state to encrypted durable current-state storage without creating a revision stream.

## Inputs

```text
CheckpointCandidate
  DraftId
  SnapshotSequence
  CurrentText (transient reference)
  SafeMatchMetadata
  CreatedAtUtc
  UpdatedAtUtc
  ExpiresAtUtc
```

## Algorithm

```text
1. reject stale/cancelled generation before protection
2. serialize DraftPayloadV1
3. protect with IDraftProtector
4. immediately clear mutable plaintext byte buffer where practical
5. build ProtectedDraftRecordV1
6. enqueue serialized repository write
7. repository performs monotonic sequence transaction
8. interpret Inserted / Updated / IdempotentNoChange / StaleIgnored
9. release transient buffers/references
```

## Important race rule

Protection can finish out of order. Repository monotonic `SnapshotSequence` is the final authority. An older already-encrypted record must still be rejected as `StaleIgnored`.

## Coalescing before protection

When safe, pending not-yet-protected checkpoints for the same `DraftId` coalesce to the newest sequence. This reduces DPAPI calls and disk churn. Do not retain an in-memory queue of old plaintext snapshots to implement coalescing.

## Failure

- protect failure: no repository call;
- repository failure: current in-memory draft remains current; future bounded checkpoint may retry;
- stale upsert: not an error and never rewinds in-memory committed sequence;
- cancellation after DPAPI but before repository: ciphertext may be discarded; plaintext fallback forbidden.

## Metrics

Allowed metrics are counts/timings only, e.g. checkpoint result category and latency. No payload sizes at exact high-cardinality precision unless separately justified.
