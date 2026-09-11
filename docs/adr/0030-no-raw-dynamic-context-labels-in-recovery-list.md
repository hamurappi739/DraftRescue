# ADR 0030 — No raw dynamic context labels in MVP Recovery list

**Status:** Accepted

## Decision

The persisted/current Recovery-card metadata does not include raw site/domain, page/window title, conversation/contact name, document filename, accessibility label, or field label solely for presentation. Cards use static application identity plus coarse `DraftPresentationKind` and timestamps.

## Rationale

Keyed matching fingerprints are intentionally non-reversible. Persisting extra raw browsing/window metadata just to render friendlier labels would expand privacy exposure and undermine metadata minimization.

## Consequence

Multiple drafts from the same app may look similar until explicit Preview. A future encrypted presentation envelope requires a separate usability/privacy ADR.
