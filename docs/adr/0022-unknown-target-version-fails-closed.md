# ADR 0022 — Security-sensitive target compatibility is not assumed across unknown versions

**Status:** Accepted

## Decision

App/profile support records an actually certified target version/range. Unknown or incompatible target versions do not automatically inherit security-sensitive guarantees, especially browser private-mode and secure-field behavior.

## Consequences

- external app updates may temporarily reduce support;
- certification is repeated for meaningful compatibility changes;
- safe failure is preferred to silent capture expansion.
