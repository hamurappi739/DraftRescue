# Phase 3 Exit Criteria

Phase 3 is GO only when all are true.

## Functionality

- synthetic allowed TextPattern target produces a complete bounded snapshot;
- certified synthetic ValuePattern target produces a snapshot;
- one logical field updates one current in-memory draft;
- ordinary non-empty edits replace current state without revision history;
- empty stabilization behaves according to spec.

## Privacy/security

- every read requires a successfully claimed one-read capability;
- no `GetText(-1)` exists in production;
- no generic LegacyIAccessible/keyboard/clipboard fallback;
- no persistence, Preview, Copy, Restore or network content path exists;
- canary absent from logs/errors/diagnostic artifacts;
- oversized content never becomes partial draft state.

## Correctness

- out-of-order/stale read cannot overwrite newer state;
- failed/timeout/cancelled read does not mutate tracker;
- capability cannot be reused after any read attempt;
- exact text is preserved without trim/Unicode/newline normalization;
- tracker holds one current snapshot only.

## Tests

All `READ-*`, `SNAP-*`, `P3F-*` tests in the canonical registry pass, plus applicable `CAP-*`, `SEC-*`, `ARC-*` regression tests.

## Operational

No unbounded growth during a 30-minute synthetic editing soak; no content-bearing logs; UIA worker remains responsive under provider faults.
