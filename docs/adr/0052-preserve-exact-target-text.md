# ADR 0052 — Preserve Exact Target Text

**Status:** Accepted

## Decision

The generic capture path preserves target text exactly; no trimming, Unicode normalization, newline rewriting, or semantic cleanup.

## Rationale

Recovery should reproduce the user's draft, not reinterpret it.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
