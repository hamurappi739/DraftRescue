# ADR 0047 — Bounded Textpattern Reads

**Status:** Accepted

## Decision

Production TextPattern reads always use a finite maxLength (`configuredLimit + 1`); `GetText(-1)` is forbidden.

## Rationale

UI Automation exposes a bounded GetText API, so unbounded document reads are unnecessary.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
