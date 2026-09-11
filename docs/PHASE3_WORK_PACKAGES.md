# Phase 3 Work Packages

## WP-3.1 — contracts only
Add transient types/results/abstractions. No UIA content implementation. Build/tests then stop.

## WP-3.2 — TextPattern bounded reader
Synthetic document target only. Finite `GetText(limit+1)`. No ValuePattern or tracker changes.

## WP-3.3 — certified ValuePattern reader
Synthetic single-line target only. Explicit profile strategy; no fallback chain.

## WP-3.4 — race-safe snapshot envelope
Generation/sequence/late-result checks. No persistence.

## WP-3.5 — in-memory tracker
One current snapshot per logical draft; no history/revisions.

## WP-3.6 — empty candidate stabilization
Two authorized reads + monotonic delay baseline; failure is not empty.

## WP-3.7 — non-leakage/fault/soak
Canary scans, forbidden-sink spies, timeout/oversize/stale tests.

## WP-3.8 — gate
Run Phase-3 registry, architecture guards, scope scan; document evidence and stop before Phase 4.
