# ADR 0051 — Oversize Is Failure Not Partial Draft

**Status:** Accepted

## Decision

Oversized target text returns a typed `TooLarge` result and never becomes a silently truncated draft.

## Rationale

Partial recovery presented as complete would violate recovery correctness.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
