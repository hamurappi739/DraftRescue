# ADR 0026 — Ambiguous profile resolution fails closed

**Status:** Accepted

## Decision

If more than one eligible production profile matches the same live app identity/version, runtime returns ambiguity/unsupported and release validation fails. File order/first-match/priority does not silently choose one.
