# ADR 0006 — Restore is fail-closed and user-initiated

**Status:** Accepted.

## Decision

DraftRescue never automatically injects a recoverable draft when a window appears. Direct Restore requires explicit user action, a fresh security classification, and a strong multi-signal target match.

Ambiguous or unsupported targets fall back to Preview/Copy rather than “force restore.”

## Consequences

- wrong-field injection risk is prioritized over convenience;
- recovery data remains useful even when direct restore is unavailable;
- every restore adapter requires explicit capability and mismatch tests.
