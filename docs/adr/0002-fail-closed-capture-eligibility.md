# ADR 0002: Capture eligibility fails closed

**Status:** Accepted for Phase 0

## Decision

A field/context may be persisted only after an explicit `Allowed` decision. Secure, private, unsupported, and uncertain decisions are all non-persistable.

## Why

The canonical requirement says that when the system is uncertain, it is safer not to save. Encoding this as a value-object invariant makes future accidental permissive behavior harder.

## Consequences

- Early versions may miss some recoverable drafts.
- Privacy takes precedence over capture coverage.
- Detection improvements can increase coverage later without weakening the default.
