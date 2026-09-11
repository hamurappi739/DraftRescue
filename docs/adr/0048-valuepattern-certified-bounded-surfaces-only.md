# ADR 0048 — Valuepattern Certified Bounded Surfaces Only

**Status:** Accepted

## Decision

ValuePattern content reads are allowed only for explicitly certified bounded single-line surfaces.

## Rationale

ValuePattern returns the complete value and offers no API-side max-length bound.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
