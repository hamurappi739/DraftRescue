# ADR 0005 — Event-driven observation without a global keyboard hook

**Status:** Accepted architecture direction; Windows primitives still require Phase 1 validation.

## Context

DraftRescue needs active application/field awareness but must not drift toward keylogger-like architecture.

## Decision

The first Windows observation implementation will evaluate foreground/focus accessibility events and targeted UI Automation metadata. A global keyboard hook is not part of the default architecture.

Content snapshots, when eventually required, must be requested only for an already allowed candidate field rather than reconstructed from a system-wide keystroke stream.

## Consequences

- reduces collection of unrelated input;
- aligns privacy boundary with field-level recovery;
- requires per-framework accessibility testing;
- some applications may remain unsupported rather than falling back to broad keyboard capture.
