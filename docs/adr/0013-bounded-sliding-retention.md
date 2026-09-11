# ADR 0013 — Bounded Sliding Retention

**Status:** Accepted

## Decision

MVP retention is sliding from the latest eligible update: `ExpiresAt = UpdatedAt + RetentionDuration`. Default is 30 minutes. Custom retention is bounded to 1 minute through 24 hours. There is no "forever" value and no additional absolute-lifetime cap in MVP.

## Why

Sliding retention preserves long active writing sessions while the 24-hour maximum duration prevents a dormant record from becoming long-term history.

## Consequences

- lengthening retention never resurrects deleted/expired drafts;
- shortening retention may immediately expire existing records;
- a continuously edited draft can remain live beyond 24 wall-clock hours because each eligible update moves expiry; once editing stops, it expires within the selected duration.
