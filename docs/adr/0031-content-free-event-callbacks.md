# ADR 0031 — Event callbacks are content-free and non-blocking

## Status
Accepted.

## Decision
WinEvent/UIA callbacks only enqueue bounded `ObservationEnvelope` data. They do not traverse UIA trees, read text-like properties, read content, persist data, or update UI synchronously.

## Rationale
Provider callbacks occur on sensitive threads and can be bursty. Keeping callbacks tiny protects privacy, responsiveness, and shutdown behavior.

## Consequences
A reconciliation worker performs all provider-bound metadata work. Queue overload coalesces to latest-context reconciliation rather than retaining an event history.
