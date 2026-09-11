# ADR 0019 — MVP app profiles ship with the signed application release

**Status:** Accepted

## Decision

MVP does not download executable support/profile policy from a server. App profiles are versioned repository/package assets released with the signed app.

## Consequences

- profile changes follow normal release review;
- core product remains fully local/offline;
- no remote policy channel can silently broaden capture surface;
- faster profile hot-fixes are deferred until a signed remote-policy design exists.
