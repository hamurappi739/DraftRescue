# Performance & Resource Budgets

**Status:** engineering targets to prevent DraftRescue from becoming an intrusive background process. Exact values may be tightened after measurements.

## 1. Principle

The application is idle most of the time. Architecture should be event-driven and bounded rather than continuously polling accessibility trees or scanning all windows.

## 2. Phase 1 observation direction

Prefer:

- foreground/focus event notifications;
- targeted inspection of the active candidate context;
- cancellation when focus/context changes;
- no recursive full-desktop tree scans on a timer.

Windows provides foreground-window lookup and WinEvent hooks; these are suitable primitives to evaluate before considering polling.

## 3. Initial budgets

These are engineering targets, not marketing guarantees:

### Idle

- no continuous high-frequency timer;
- average CPU should approach process-idle noise on a normal desktop;
- no repeated UI Automation queries while foreground context is unchanged and inactive;
- no disk writes while there is no active eligible draft state change.

### Active typing in one supported field

- observation work should remain lightweight enough to be imperceptible;
- expensive UIA traversal must be bounded to the relevant active subtree;
- persistence writes must be coalesced;
- UI rendering must not occur per character unless recovery UI is actually open.

### Memory

- retain only active/recoverable states needed by policy;
- no unbounded event queues;
- no historical text revisions;
- dispose Windows event hooks/COM resources deterministically.

## 4. Backpressure

If UI Automation/provider events arrive faster than processing:

- coalesce by current field/context;
- drop superseded metadata events where safe;
- never queue every character/event indefinitely;
- preserve privacy gating correctness over completeness.

## 5. Timeout policy

External accessibility/provider calls can hang or become slow.

Use bounded cancellation/timeouts at adapter boundaries. Timeout => Uncertain/Unsupported, not a permissive fallback.

## 6. Measurement scenarios

Each relevant phase should collect:

- idle CPU over several minutes;
- active typing CPU;
- working set before/after repeated focus changes;
- handle/thread count stability;
- UI Automation query latency percentiles;
- event count during normal desktop use;
- persistence writes per minute under sustained typing;
- cleanup behavior after hours of idle use.

Do not include raw draft contents in performance traces.

## 7. Leak tests

Run repeated cycles of:

- focus Notepad/editor;
- focus another app;
- close/reopen target;
- start/stop observation.

Assert no monotonic growth in WinEvent hooks, COM objects, handles, subscriptions, tasks, or unbounded collections.
