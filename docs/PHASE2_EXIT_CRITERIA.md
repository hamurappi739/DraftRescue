# Phase 2 Exit Criteria

Phase 2 may be marked complete only when all conditions below are satisfied on Windows with .NET 8.

## Functional gate

- typed security evidence model exists;
- policy evaluator is deterministic and I/O-free;
- native/synthetic protected control denies;
- credential/PIN/payment synthetic surfaces deny;
- missing/unknown/provider-failure states deny;
- positive synthetic ordinary surface can produce exactly one capability;
- capability is generation/profile/binding-bound;
- capability is one-read and short-lived;
- stale/expired/reused capability validation fails.

## Privacy gate

- Phase 2 production code performs zero target-content reads;
- no raw field content/dynamic UIA strings in logs/tests/artifacts;
- `Denied => capability issue count 0` for every negative fixture;
- content-reader spy count is zero for every Phase 2 test;
- no browser capture is enabled;
- no keyboard capture is added.

## Architecture gate

Pass:

- ARC-005
- ARC-006
- ARC-007
- SEC-014..SEC-023
- CAP-001..CAP-008
- P2F-001..P2F-006

## Stress/fault gate

- 10,000 repeated allow/deny evaluations have deterministic results and bounded memory;
- capability registry has no unbounded tombstone/history growth;
- target destruction during classification fails closed;
- concurrent duplicate claims yield exactly one winner;
- fake monotonic deadline tests pass without wall-clock sleeps.

## Review artifacts

Produce:

- schema-valid Phase 2 test matrix result;
- native fixture evidence record;
- source/API guard scan report;
- invariant/test traceability report;
- list of target profiles still Experimental.

## Stop

Do not implement Phase 3 content reading in the same change/work package used to close Phase 2.
