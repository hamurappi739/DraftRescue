# ADR 0033 — Bounded event coalescing converges on latest context

## Status
Accepted.

## Decision
DraftRescue does not retain/replay a lossless desktop focus-event history. Duplicate/storm events are coalesced; overload preserves one latest-context reconciliation marker, and security capability is revoked during uncertainty.

## Rationale
The product needs current context, not historical event telemetry. A lossless event log would increase memory/privacy risk without improving recovery semantics.
