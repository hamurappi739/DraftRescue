# Empty and Clear Semantics — Phase 3

An empty read is valid content state, but one empty read must not immediately destroy the recoverable in-memory draft.

## States

```text
NonEmptyCurrent
  -> EmptyCandidate(first observed empty)
  -> EmptyConfirmed(stable empty according to target profile rule)
```

A non-empty read during `EmptyCandidate` cancels the candidate and becomes current.

## Baseline stabilization

For the initial synthetic/native target experiment, an empty state is considered stable only after both:

1. a second independently authorized read observes empty; and
2. at least 300 ms monotonic time has elapsed since first empty observation.

This is an experiment baseline, not a universal completion/submission signal.

## Important distinction

Stable empty may mean manual clear, submit, navigation, provider recreation, or application logic. Phase 3 only models the current in-memory text state. Terminal deletion semantics remain governed by later target-specific completion rules.

## Failure behavior

Timeout/stale/unsupported/error is **not empty** and cannot confirm clear.
