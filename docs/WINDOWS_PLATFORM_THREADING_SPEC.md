# Windows Platform Threading Specification

**Status:** accepted architecture baseline based on Microsoft UI Automation threading guidance; exact implementation mechanics are Phase-1 work.

## 1. Never run desktop-wide UI Automation work on Avalonia UI thread

All UI Automation calls that inspect external desktop elements belong on a dedicated non-UI worker owned by `DraftRescue.Platform.Windows`.

The Avalonia dispatcher receives only already-sanitized application presentation models/events.

## 2. COM apartment baseline

The dedicated UI Automation worker should initialize COM as a Multithreaded Apartment (MTA). Event-handler registration/removal is owned by the same platform subsystem/thread discipline.

Do not scatter UIA subscriptions across arbitrary thread-pool tasks.

## 3. Worker responsibilities

The UIA/platform worker may:

- own UI Automation client objects;
- subscribe/unsubscribe relevant automation events;
- locate focused candidate elements;
- query metadata/capabilities;
- perform approved read/write pattern operations after application authorization;
- translate provider errors into typed platform results.

It may not:

- decide product-level secure/privacy policy;
- persist draft data;
- update UI controls directly;
- log user text.

## 4. WinEvent lifetime

If `SetWinEventHook` is used, hook registration, callback lifetime, and unhooking are explicit resources. Out-of-context callbacks are normalized into small metadata events and passed to the observation coordinator.

The process must not exit with hooks/event registrations intentionally left dangling.

## 5. Cross-thread objects

Avoid retaining/marshaling raw UI Automation element objects broadly across application layers. Prefer short-lived platform tokens/handles resolved and validated inside the platform worker.

Reason: accessibility element lifetime/apartment/provider state can become stale and should not become domain identity.

## 6. Async bridge

Application-facing APIs can remain `Task`/`IAsyncEnumerable` based while the platform implementation serializes appropriate UIA work onto its owned worker.

Cancellation from application layer:

- removes queued work when possible;
- marks late completion stale;
- never changes a deny/unknown to allow.

## 7. Timeout strategy

Because cross-process providers can hang or respond slowly, UIA calls must have application-level budgets and stale-generation rejection. A timeout does not forcibly abort arbitrary COM provider code in an unsafe way; implementation may need isolation/backoff patterns discovered in Phase 1.

## 8. UI thread boundary

Allowed flow:

```text
Windows/UIA worker
  -> sanitized metadata/application event
  -> Application state/use case
  -> ViewModel presentation model
  -> Avalonia Dispatcher/UI
```

Forbidden flow:

```text
Avalonia Button/ViewModel
  -> raw AutomationElement traversal
  -> provider call on UI thread
```

## 9. Tests/measurement

Phase 1 must verify:

- DraftRescue UI remains responsive while target provider is delayed;
- own-process event exclusion prevents recursive observation;
- add/remove event handlers are deterministic;
- repeated start/stop does not leak handles/subscriptions;
- stale worker results are rejected by context generation;
- shutdown does not deadlock waiting for provider calls.
