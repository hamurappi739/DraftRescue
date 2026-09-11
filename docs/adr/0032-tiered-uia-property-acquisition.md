# ADR 0032 — Tiered UI Automation property acquisition

## Status
Accepted.

## Decision
Generic pre-classification UIA access is allowlisted by property-safety tier. Tier A structural properties may be queried; Tier B requires profile/experiment justification; Tier C dynamic text-like metadata is not generically queried; Tier D content is unavailable until an `AllowedFieldHandle` exists.

## Rationale
UI Automation metadata is not automatically non-sensitive. Provider-defined strings such as Name/HelpText may expose user/context content.
