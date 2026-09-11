# ADR 0035 — Phase 1 has zero target-content reads

## Status
Accepted.

## Decision
Phase 1 (Active App + Field Detection) is metadata-only. Its production/experiment paths must not read ValuePattern/TextPattern/LegacyIAccessible text or equivalent target content.

## Rationale
Separating observation from content access makes classify-before-read enforceable and measurable.
