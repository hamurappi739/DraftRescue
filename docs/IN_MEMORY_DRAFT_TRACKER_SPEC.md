# In-Memory Draft Tracker Specification

## Purpose

Phase 3 proves current-state tracking without persistence.

## State per active logical draft

```text
DraftId (process-local prototype identity)
ContextFingerprintRef / logical identity token
ContextGeneration
LatestAcceptedSnapshotSequence
CurrentText
UpdatedAtUtc
EmptyCandidateState
```

## ApplySnapshot

A snapshot is accepted only if:

- logical identity matches;
- generation is current;
- sequence is newer than latest accepted sequence;
- result is complete/non-truncated;
- no terminal state has already invalidated the draft.

Outcomes:

- `Created`
- `Updated`
- `Unchanged`
- `StaleIgnored`
- `IdentityMismatch`
- `EmptyCandidateStarted`
- `EmptyCandidateConfirmed`

## History prohibition

Tracker stores one current text value. Updating replaces the previous reference; it does not append revisions/deltas/keystrokes.

## Process restart

All Phase-3 draft state is lost on process exit. That is intentional until Phase 4 encrypted persistence exists.
