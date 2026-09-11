# WinEvent and UIA Event Routing Specification

## Purpose

Define the content-free event plane that detects context changes without collecting typed input.

## Canonical rule

**Callbacks signal work; callbacks do not inspect content.**

No callback may:

- read ValuePattern/TextPattern/LegacyIAccessible values;
- traverse a deep UIA subtree;
- query `Name`, `HelpText`, `ItemStatus`, or other dynamic text-like properties generically;
- touch persistence;
- invoke encryption;
- update UI directly;
- synchronously wait on another process.

## Event envelope

```text
ObservationEnvelope
- ObservationSequence : ulong
- Source              : ForegroundWinEvent | UiaFocusChanged | Reconcile
- ObservedAtMonotonic : monotonic timestamp
- ProcessId?          : int
- ThreadId?           : int
- NativeWindowHandle? : nint
- RawEventCode?       : uint
```

The envelope contains no window title, field name, URL, automation Name, typed content, or clipboard content.

## Registration

### WinEvent

Baseline research subscription:

- `EVENT_SYSTEM_FOREGROUND` only at first;
- `WINEVENT_OUTOFCONTEXT`;
- `WINEVENT_SKIPOWNPROCESS`;
- `idProcess = 0`, `idThread = 0` for current desktop observation.

Do not subscribe to broad object-event ranges merely because they are available.

### UIA focus

Use a global focus-changed handler on the dedicated MTA worker. If managed UIA proves operationally unreliable, Phase 1 may compare the native COM API, but the architecture boundary remains the same.

## Queueing

- single bounded observation channel;
- callbacks perform non-blocking `TryWrite`/equivalent;
- queue payload is content-free;
- duplicate/coalescible items may be collapsed;
- overflow triggers a `ReconcileLatestContext` marker, never blind replay of an unbounded backlog;
- no event backlog is persisted.

## Ordering

`ObservationSequence` is process-local monotonic ordering, not a statement about provider truth. Resolution creates/advances `ContextGeneration` only after reconciliation determines that the logical context changed.

## Coalescing key

Use only coarse identifiers available in the envelope:

```text
(source, processId, nativeWindowHandle, rawEventCode)
```

Do not include raw title/name strings in a dedup key.

## Reconciliation rule

When events conflict:

1. discard envelopes older than the last completed generation boundary when clearly stale;
2. schedule one bounded latest-context reconciliation;
3. inspect current foreground/focused metadata at execution time;
4. normalize;
5. compare with current normalized identity;
6. only then advance `ContextGeneration`.

## Own-process rule

DraftRescue's PID is ignored both at hook configuration and again after normalization. Defense in depth is intentional.

## Event storm behavior

A storm must reduce accuracy conservatively, not reduce privacy. If overload prevents reliable reconciliation:

- drop intermediate event history;
- keep one latest-reconcile marker;
- classify the interim context as unknown/not eligible;
- never reuse the previously allowed field capability across the uncertainty interval.

## Disposal

Unhooking/removing handlers is idempotent. Shutdown does not wait for provider callbacks indefinitely. Pending work receives cancellation and stale generations are discarded.
