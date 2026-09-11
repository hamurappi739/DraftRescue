# Windows Observation Research Plan

**Status:** Phase 1 experimental plan. This is not implementation approval.

## 1. Goal

Determine the smallest event-driven Windows mechanism that reliably identifies:

- foreground application/window;
- currently focused candidate editable element;
- minimal metadata needed for Phase 2 security gating;

without implementing global keyboard capture.

## 2. Candidate primitives to evaluate

### Foreground window

`GetForegroundWindow` can provide the current foreground HWND. It may return null during transitions, so null is a normal transient state, not an error requiring aggressive retries.

### Event-driven foreground/focus notifications

`SetWinEventHook` can subscribe to selected accessibility/system events out-of-context. The hook owner requires a message loop and must manage callback lifetime/resources correctly.

Prefer narrow event ranges and skip DraftRescue's own process where appropriate.

### UI Automation

UI Automation exposes element properties, tree structure, and control patterns. Text-bearing controls vary: some expose TextPattern, others ValuePattern, and writable capability differs by framework/control.

`IsPassword` is a key protected-content deny signal, but not sufficient by itself for all credential/security cases.

## 3. Explicitly rejected Phase 1 direction

Do not start with:

- low-level keyboard hooks;
- Raw Input used as a global typing stream;
- polling every window/control repeatedly;
- full-desktop accessibility tree snapshots;
- reading every focused element's value before classification.

## 4. Experiment 1 — foreground app identity

Measure:

- correctness across Notepad, Explorer, browser, DraftRescue itself;
- null/transient HWND behavior;
- process identity resolution;
- elevated/non-elevated boundaries;
- event volume while rapidly switching apps.

Output: normalized metadata only. No field text.

## 5. Experiment 2 — focused element metadata

For Notepad and a small controlled test harness, inspect only metadata needed to answer:

- editable?
- enabled?
- read-only?
- control type?
- IsPassword/protected?
- supported patterns?
- stable identifiers/ancestry signals?

Do not read Value/TextRange content yet.

## 6. Experiment 3 — event stability

Measure whether focus/foreground events are sufficient or if a bounded fallback probe is necessary.

Record:

- missed transitions;
- duplicate events;
- latency;
- provider failures;
- CPU/handle/thread behavior.

## 7. Phase 1 output contract

The accepted Phase 1 adapter should produce a metadata-only candidate context plus capability flags. Text acquisition is deliberately a separate interface behind Phase 2 gating.

## 8. Success gate

Proceed to secure field work only when:

- no keyboard hook exists;
- foreground/focused candidate metadata works for the controlled target;
- resources clean up correctly;
- unsupported/failure states are explicit;
- no draft content is logged or persisted.
