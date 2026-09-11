# ADR 0039 — AllowedFieldHandle Is One-Read Capability

**Status:** Accepted

## Decision

`AllowedFieldHandle` authorizes at most one target-content read. Claim is atomic and consumes the handle even if the subsequent read fails/cancels/times out.

## Why

Long-lived reusable permission turns an old metadata classification into ambient trust and increases TOCTOU exposure.

## Consequences

Each later snapshot read requires fresh metadata security classification and a new capability. Phase 2 still contains no content reader.
