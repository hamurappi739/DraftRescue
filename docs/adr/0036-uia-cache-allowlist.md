# ADR 0036 — UIA cache requests use audited allowlists

## Status
Accepted.

## Decision
Any UI Automation cache request must contain only properties/pattern flags explicitly approved for that phase/profile. Generic cache requests must never prefetch dynamic text or field content for convenience.

## Rationale
Caching is itself data retrieval. A convenient broad cache could violate the privacy boundary before classifier code runs.
