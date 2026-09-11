# ADR 0049 — No Generic Legacyaccessible Content Fallback

**Status:** Accepted

## Decision

No generic LegacyIAccessible/keyboard/clipboard fallback is used to recover content when the certified UIA read strategy fails.

## Rationale

Fallback ladders broaden privacy and compatibility risk and hide unsupported targets.

## Consequences

Future implementation and tests must treat this as canonical unless the user explicitly reopens the decision.
