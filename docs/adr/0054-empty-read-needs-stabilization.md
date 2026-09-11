# ADR 0054 — Empty Read Needs Stabilization

**Status:** Accepted

## Decision

A single empty read does not terminally clear a tracked draft; empty state requires a second authorized observation and monotonic stabilization baseline.

## Rationale

Provider recreation/races can transiently surface empty text.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
