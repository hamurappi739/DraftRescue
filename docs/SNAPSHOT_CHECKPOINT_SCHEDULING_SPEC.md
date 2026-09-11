# Snapshot Acquisition and Checkpoint Scheduling Specification

**Status:** algorithm baseline; exact timing constants require Phase 3/4 measurement.

## 1. Objective

Persist a useful recent current snapshot without turning every edit event into durable typing history, excessive disk I/O, or a hot polling loop.

## 2. Separation of concerns

- **observation event** says a relevant context may have changed;
- **snapshot acquisition** reads the current complete field value after eligibility authorization;
- **dirty state** means in-memory current snapshot differs from the last committed protected snapshot;
- **checkpoint** replaces the single durable current snapshot.

Never persist a stream of edit deltas/keystrokes.

## 3. Scheduling model

Use a hybrid policy:

1. short trailing debounce after eligible changes to avoid checkpointing every rapid edit;
2. bounded maximum dirty age so continuous typing still checkpoints periodically;
3. immediate/bounded checkpoint triggers for selected lifecycle events such as target loss or graceful application exit, but only for already eligible state;
4. no text re-read during Windows shutdown just to obtain a fresher value.

Exact debounce and max-dirty-age constants are experimental parameters, not product promises.

## 4. Coalescing

Multiple signals for the same `DraftId`/`ContextGeneration` coalesce. A newer acquired snapshot supersedes older pending snapshots.

Pending work for a stale generation is canceled/dropped.

## 5. Snapshot equality

If newly acquired text exactly matches the current in-memory snapshot, do not create extra work merely due to focus/UIA events.

Text equality is local to the same authorized logical draft; do not use plaintext equality for cross-draft dedupe.

## 6. Empty state

Stable empty value follows `SUBMISSION_AND_CLEAR_DETECTION.md`. Do not checkpoint the previous non-empty value again after intentional stable clear.

## 7. Failure/backoff

Snapshot read timeout/provider failure:

- does not make the target Allowed;
- does not delete the previous valid recoverable snapshot;
- does not loop aggressively;
- emits structural failure telemetry/logging only.

## 8. Privacy consequence

At most one current durable payload exists per logical draft. Frequent replacements must not preserve old ciphertext rows/files as an intentional feature.

## 9. Measurements before locking constants

Measure with synthetic fixtures:

- 30 seconds continuous typing;
- 5 minutes intermittent typing;
- paste large body;
- rapid focus switching;
- provider latency/hang;
- SSD write count / CPU / allocations;
- kill-process recovery freshness.

Choose constants that balance useful recovery freshness against resource use; record final values in a later ADR.
