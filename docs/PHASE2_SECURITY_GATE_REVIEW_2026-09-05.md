# Phase 2 Security Gate Review — 2026-09-05

## Scope

WP-2.1 through WP-2.7 are implemented: typed security signals, deterministic
fail-closed policy evaluation, synthetic positive predicates, an opaque
single-use `AllowedFieldHandle` issuer/validator, and a metadata-only native
Windows Forms/UIA secure-field fixture. No target-content reader, clipboard
path, keyboard hook, persistence, or restore path was added.

## Evidence

`artifacts/phase2-security-gate-wp28/PHASE2-SECURITY-GATE.json` records the
metadata-only matrix of 24 synthetic cases: 23 negative controls and one
ordinary-field positive control. All 24 cases pass; every case reports zero
content-read invocations. The same gate now runs a real Windows Forms/UIA
fixture with three empty controls: ordinary editable is allowed and receives a
capability, password and read-only controls are denied. The native fixture
also reports zero content-read invocations. The source/API guard scanned five
security source files and found zero forbidden content APIs or reader
implementations.

The automated test suite passed `82/82` at the Phase-2 boundary. The solution builds on .NET 8 with zero
warnings and zero errors. Phase 1 and Phase 2 content-boundary guards pass.

## Security properties now enforced

- `Unknown`, `Unavailable`, `Failed`, stale, timeout, private, unsupported,
  sensitive-purpose, and incompatible-version signals deny.
- `IsPassword=false` and `Editable=true` alone never allow.
- Positive allow requires a resolved certified profile and an exactly satisfied
  product-owned predicate.
- Deny precedence is deterministic and stable error codes are content-free.
- Capability issuance rechecks candidate, binding, generation, profile and
  revision.
- Capability age uses a monotonic clock: 1000 ms default, 2000 ms hard cap.
- Claim is atomic and single-use; expiry, revocation, generation or binding
  changes prevent a claim.
- `AllowedFieldHandle` is process-local, non-serializable, non-persistable and
  cannot authorize Restore.

## Remaining Phase 2 work

The WP-2.7 native fixture and WP-2.8 executable fault/negative review both pass.
The fixture and matrix do not certify arbitrary Windows controls or any browser
surface; browser capture remains unsupported. Phase 2 is therefore closed at
its defined boundary. Phase 3 has since started in a separate bounded gate; rerunning
this Phase-2-only guard after that transition is expected to report the certified
Phase-3 reader as outside the Phase-2 boundary.

## WP-2.8 fault/negative evidence

`artifacts/phase2-security-gate-wp28/fault-run/PHASE2-FAULT-HARNESS.json` is an
executable, metadata-only run rather than a static expectation table. It passes
all 24 cases (23 negative, one positive), performs 10,000 deterministic
allow/deny evaluations, 10,000 issue/revoke cycles with no retained registry
state, and 128 deny-precedence permutations with one stable result. Allocated
bytes remain below the 16 MiB bounded-memory threshold. Concurrent duplicate
claims have exactly one winner, and fake monotonic-clock expiry is exercised
without wall-clock sleeps.

## Exit decision

**Phase 2 gate: Pass.** Functional, privacy, architecture, stress/fault and
review-artifact criteria are satisfied. No production content reader,
clipboard/keyboard fallback, persistence or restore path was introduced.

## Target profiles still Experimental

- Notepad (reconnaissance only; no support certification).
- Chrome and Edge browser examples (private-mode and sensitive-purpose
  certification not complete; browser capture remains unsupported).
- The Windows Forms fixture is test-only and is not a product support profile.

## Invariants

P-002, P-003, P-004, P-005, P-006, P-017, P-025, P-026, P-028, P-030, P-031,
P-032, P-033, P-034, P-035, P-036, P-037; C-019, C-020, C-026, C-027, C-028,
C-029, C-030, C-031, C-032, C-033.
