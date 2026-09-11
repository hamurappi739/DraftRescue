# Focus/Event Deduplication and Backpressure Specification

## Problem

Windows/UIA providers can emit duplicate, bursty, delayed, or semantically redundant focus/foreground events. DraftRescue must not turn that into hot polling, an unbounded queue, or stale security capability reuse.

## Model

Two layers:

1. `ObservationEnvelope` — cheap raw content-free event.
2. `ContextReconcileRequest` — coalesced instruction to determine the latest current context.

## Rules

- Do not replay every historical focus event.
- At most one reconciliation job runs at a time.
- While one runs, additional relevant events set `reconcileAgain = true` and update the latest sequence marker.
- When the job completes, if `reconcileAgain` is true, run once more against current state.
- Stale results are rejected by `ContextGeneration`/observation sequence checks.
- Queue overflow produces one reconcile marker, not lossless retention of the storm.

## Suggested initial timing constants for experiments only

These are measurement starting points, not final product constants:

- coalescing window: 25 ms;
- max callback work: target < 1 ms p99 in synthetic harness;
- provider metadata timeout budget: 250 ms initial experiment ceiling;
- exponential backoff after repeated provider failure: start 250 ms, bounded to 5 s;
- reset backoff after a confirmed healthy context transition.

Final numbers must be chosen from `SNAPSHOT_TIMING_EXPERIMENT_PLAN.md` / performance evidence, not copied blindly.

## Security behavior during uncertainty

If the current allowed field loses focus or the system enters an overload/reconcile interval, revoke the allowed capability immediately. Do not continue reading old target content until the latest context is freshly classified.

## Test obligations

- duplicate same-element focus event;
- foreground+focus pair for one transition;
- 1000-event burst;
- alternating A/B focus at high rate;
- provider resolves A after B became current;
- queue saturation;
- target process exits during reconciliation.
