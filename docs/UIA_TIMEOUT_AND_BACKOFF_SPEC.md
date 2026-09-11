# UI Automation Timeout, Cancellation, and Backoff Specification

**Status:** mandatory reliability/safety design for Windows platform layer.

## 1. Principle

Third-party accessibility providers can be slow, hung, reentrant, or disappear mid-call. DraftRescue must treat all external UIA/provider calls as unreliable boundaries.

## 2. Thread ownership

Desktop-wide UIA work runs on the dedicated non-UI MTA worker defined by ADR 0016. Avalonia UI thread never waits indefinitely for target-process accessibility calls.

## 3. Bounded operations

Every logical operation has a deadline/cancellation context, including:

- focused element lookup;
- property batch read;
- pattern acquisition;
- text snapshot read;
- recovery target lookup;
- restore write/verification.

Exact timeouts require measurement. No API wrapper may expose an unbounded wait as the normal path.

## 4. Fail-closed semantics

Timeout means `Unavailable/Uncertain`, never permission to skip classification or use a lower-level broad fallback.

## 5. Backoff

Repeated failures for the same process/profile should trigger bounded exponential or tiered backoff with jitter/upper bound. The goal is preventing CPU churn and repeatedly hanging on a broken provider.

Backoff metadata contains only structural keys (process/profile identity token), never draft text.

## 6. Recovery from backoff

Backoff may reset on:

- new process instance;
- profile version change;
- meaningful time interval;
- explicit developer/test reset.

A mere stream of repeated focus events should not defeat backoff.

## 7. Provider crashes / stale COM objects

Treat COM/UIA exceptions and stale-element results as expected boundary failures. Drop ephemeral refs, advance/re-evaluate generation as needed, and do not log provider-returned text values.

## 8. Testing

Synthetic harness must support:

- delayed property read;
- hung read;
- exception on read;
- element disappears between property batches;
- write hangs;
- verification hangs;
- thousands of repeated focus events while provider remains broken.

Pass condition includes bounded CPU/threads/handles and responsive DraftRescue UI.
