# ADR 0037 — Browser form capture is not a generic early target

## Status
Accepted.

## Decision
Browser editable surfaces remain unsupported for capture until a browser/version-specific path proves private-mode classification and sensitive-purpose exclusion before content read. Chrome/Edge may be researched earlier for topology only.

## Rationale
Generic UIA editability/password flags are insufficient to guarantee exclusion of credentials, payment data, and private browsing contexts.
