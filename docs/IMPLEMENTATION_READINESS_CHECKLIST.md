# Implementation Readiness Checklist

**Purpose:** decide whether a future work package is specified enough to hand to Cursor without architectural improvisation.

## Global foundation readiness

- [x] product/MVP scope exists;
- [x] privacy invariants have stable IDs;
- [x] correctness invariants have stable IDs;
- [x] layers/projects and ownership boundaries are defined;
- [x] current draft lifecycle is defined;
- [x] classification-before-read is defined;
- [x] plaintext boundaries are defined;
- [x] persistence technology baseline is chosen;
- [x] protection baseline is chosen;
- [x] retention policy is bounded;
- [x] recovery matching semantics are categorical/deterministic;
- [x] restore revalidation/verification semantics are defined;
- [x] profile resolution/version behavior is fail-closed;
- [x] use-case input/result contracts exist;
- [x] stable error taxonomy exists;
- [x] UI information architecture/component states exist;
- [x] end-to-end scenarios exist;
- [x] tests/certification approach exists;
- [x] future code ownership/layout is defined.

## Items intentionally still experiment-dependent

The following are **not** missing documentation; they must be learned from narrow Windows experiments before implementation is frozen:

- exact minimum Windows version;
- final WinEvent/UIA event combination;
- control-family read strategy (`ValuePattern`, `TextPattern`, adapter) based on real fixtures;
- secure/credential/banking metadata signals beyond hard protected-content signals;
- per-browser private-mode detector evidence;
- concrete debounce/max-dirty-age timing;
- direct Restore mechanism per target class;
- packaging/startup mechanism after packaging experiment;
- production diagnostic-log default/rotation.

Cursor must not resolve these globally inside another work package.

## Work-package Definition of Ready

Before handing one WP to Cursor:

1. WP scope names one phase/subtask;
2. required docs are listed;
3. relevant `P-*` and `C-*` invariants are listed;
4. exact expected files/projects are bounded;
5. platform assumption is either accepted or the task is explicitly an experiment;
6. acceptance tests are named by ID;
7. stop condition is explicit;
8. no unresolved cross-cutting decision is hidden inside the task.

If any item is false, keep designing/experimenting before implementation.

## Phase 1 pre-implementation readiness

- [x] content-free event envelope is defined;
- [x] WinEvent/UIA routing model is defined;
- [x] UIA property safety tiers are defined;
- [x] candidate metadata normalization contract is defined;
- [x] event coalescing/backpressure semantics are defined;
- [x] zero-content-read Phase 1 boundary is explicit;
- [x] provider fault/backoff scenarios are defined;
- [x] Notepad reconnaissance protocol is defined;
- [x] machine-readable experiment format exists;
- [x] Phase 1 tests/exit criteria are named;
- [ ] final numeric timeout/coalescing budgets are measured on Windows;
- [ ] actual WinEvent/UIA combination is validated on Windows;
- [ ] target topology observations are recorded on supported OS builds.

The unchecked items are experiment outputs, not decisions a coding agent may invent.


## Phase 4 design readiness

- [x] protected-record repository boundary specified
- [x] SQLite schema v1 specified
- [x] explicit SQLite connection policy specified
- [x] DPAPI payload format/version specified
- [x] installation fingerprint-secret lifecycle specified
- [x] checkpoint commit/race semantics specified
- [x] retention/expiry execution specified
- [x] corruption/quarantine and migration policy specified
- [x] plaintext-at-rest certification protocol specified
- [x] Phase-4 fault matrix and exit gate specified
- [ ] Windows implementation exists
- [ ] real .NET 8 Windows build/tests pass
- [ ] SQLite/DPAPI integration experiments pass

Design-ready does not mean implementation-complete.
