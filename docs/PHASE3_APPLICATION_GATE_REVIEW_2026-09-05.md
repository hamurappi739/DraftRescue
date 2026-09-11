# Phase 3 Application Gate Review — 2026-09-05

## Scope

This bounded block implements WP-3.1, WP-3.2, WP-3.3 and the application-side
parts of WP-3.4..WP-3.6: the one-shot reader contract and typed result union,
bounded certified TextPattern and ValuePattern adapters, `FieldTextSnapshot`,
generation/sequence-safe current-state tracking, and 300 ms two-observation
empty stabilization. No persistence, UI body exposure, clipboard path, restore
path, or browser path was added.

## Evidence

`artifacts/phase3-application-gate-final/PHASE3-APPLICATION-GATE.json` records
the application/reader gate. The current suite passes `112/112`; the Phase 3
source/API guard passes with zero forbidden APIs and two bounded reader
implementations (TextPattern and certified ValuePattern). Snapshot tests preserve
exact UTF-16 text while JSON output contains no canary. Tracker tests cover
create/update/unchanged, stale sequence and generation rejection, empty candidate
stabilization, cancellation of a pending empty state, and the one-current-record/
no-history invariant.

## Safety boundary

- The reader interface accepts only `AllowedFieldHandle`, `ReadBudget`, and a
  cancellation token; it accepts no raw UIA element, hwnd or process id.
- The TextPattern adapter claims the capability before resolving the target,
  requests exactly `configuredLimit + 1`, rejects oversize, and rechecks target
  currency before creating a snapshot.
- The certified ValuePattern adapter claims the capability before resolving the
  target, reads only `ValuePattern.Current.Value` on a certified single-line
  surface, applies a post-read UTF-16 cap without truncation, and rechecks target
  currency before creating a snapshot.
- Failure results contain only typed status or stable error code; they never
  carry target text.
- `ReadBudget` enforces the 65,536 default and 262,144 absolute UTF-16 limits.
- `FieldTextSnapshot` is transient, exact-text, generation/sequence stamped and
  deliberately redacts all fields from generic JSON serialization.
- `InMemoryDraftTracker` keeps one current snapshot per opaque logical field,
  rejects stale results and never stores a revision list.
- Empty is not an immediate clear: a second independently applied empty snapshot
  after 300 monotonic milliseconds is required.

## Phase 3 completion

WP-3.7 integrated reader/fault/non-leakage evidence and WP-3.8 final Phase 3
gate are recorded separately in the integrated, soak and final exit artifacts.
The complete Phase 3 exit decision is **Pass**.

## Invariants

P-002, P-003, P-004, P-005, P-006, P-017, P-025, P-026, P-028, P-030, P-031,
P-032, P-033, P-034, P-035, P-036, P-037; C-019, C-020, C-026, C-027, C-028,
C-029, C-030, C-031, C-032, C-033.
