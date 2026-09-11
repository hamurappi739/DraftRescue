# Phase 1 Windows Experiment Plan

**Status:** canonical pre-implementation research plan. Phase 1 experiments are metadata-only. They must not read, retain, copy, log, or persist target text.

## Objective

Prove that DraftRescue can observe foreground/focus context changes on Windows with bounded cost and without becoming a keyboard logger, then produce a normalized `TargetFieldCandidateMetadata` object suitable for Phase 2 security classification.

Phase 1 is successful even if no text can be read. Text reading is explicitly out of scope.

## Questions to answer

1. Can foreground transitions be observed reliably with a narrow `SetWinEventHook` subscription?
2. Can UI Automation focus events reliably identify the focused control across the initial target classes?
3. Which metadata properties are available without accessing text patterns or value-bearing properties?
4. Which providers hang, throw, or expose unstable identities?
5. How often do duplicate/out-of-order events occur?
6. Can event storms be coalesced without losing the newest context generation?
7. What minimum metadata is sufficient for later profile/security classification?

## Proposed observation sources

### Source A — WinEvent foreground

Use a narrow out-of-context WinEvent hook for `EVENT_SYSTEM_FOREGROUND` with own-process filtering. The callback emits only a tiny content-free envelope: source, event id, hwnd/process/thread ids when available, timestamp, and monotonically assigned observation sequence.

### Source B — UI Automation focus changed

Subscribe to global UI Automation focus-changed events on the dedicated MTA worker. The callback must enqueue a work item; it must not perform deep property traversal, text retrieval, tree search, logging of dynamic strings, or persistence.

### Source C — explicit reconciliation

When Source A and Source B disagree or one is absent, schedule a bounded reconciliation job on the UIA worker. Reconciliation may query only the Tier A/Tier B metadata allowlist defined in `UIA_PROPERTY_SAFETY_CLASSIFICATION.md`.

## Experiment stages

### P1-E1 — event plumbing only

- register/unregister hooks cleanly;
- prove callbacks stop after disposal;
- ignore DraftRescue's own process;
- emit content-free envelopes only;
- measure callback count, queue depth, and callback duration.

**Stop condition:** no UI Automation property read other than identifiers required to route the event.

### P1-E2 — focused element metadata

For synthetic targets and Notepad research only:

- obtain focused `AutomationElement` on MTA worker;
- cache the minimum allowlisted metadata in one bounded batch where practical;
- normalize into `TargetFieldCandidateMetadata`;
- discard the live `AutomationElement` outside the platform layer.

**Forbidden:** ValuePattern.Value, TextPattern document ranges, LegacyIAccessible value, clipboard, keystrokes.

### P1-E3 — event deduplication and generation

Test:

- repeated focus notifications for same element;
- foreground followed by focus;
- focus followed by foreground;
- rapid Alt-Tab;
- target closes before metadata resolves;
- UIA tree rebuilt while focused;
- 1000-event synthetic burst.

Expected: the latest legitimate context wins; stale asynchronous jobs are discarded by `ContextGeneration`.

### P1-E4 — provider failure behavior

Inject or simulate:

- element not available;
- COM failure;
- access denied/integrity mismatch;
- provider timeout/hang;
- malformed property value;
- process exits during lookup.

Expected: typed structural failure; never `Allowed`; no retry storm.

### P1-E5 — target reconnaissance

Run only with synthetic non-sensitive text and document results for:

1. synthetic Win32 edit control;
2. synthetic WPF/Avalonia test field;
3. current Notepad version as an experimental target;
4. Chrome and Edge only for metadata/topology reconnaissance, not capture enablement.

## Required measurements

- events/minute idle;
- events per ordinary focus switch;
- callback p50/p95/p99 duration;
- metadata job p50/p95/p99 duration;
- max queue depth during focus storm;
- dropped/coalesced event count;
- UIA timeout count;
- handle/thread growth during 30-minute soak;
- CPU while idle and under rapid focus switching;
- memory delta over soak;
- any raw dynamic string observed by diagnostics (must be zero).

## Output artifacts

Each run produces a content-free `ExperimentResult` described by `EXPERIMENT_RESULT_RECORD_FORMAT.md`. Screenshots or manual notes containing user text are prohibited. Synthetic app labels may be captured only when explicitly marked synthetic.

## Exit criteria

See `PHASE1_EXIT_CRITERIA.md`. No Phase 2 implementation should begin until Phase 1 evidence is reviewed.

## Verified Windows API facts

- `SetWinEventHook` can subscribe across processes; out-of-context delivery is queued and requires a message loop on the registering thread.
- UI Automation exposes a global focus-changed event.
- UI Automation cross-process property access can be expensive; cache requests can batch properties/patterns.

These facts are research inputs, not permission to broaden capture scope.
