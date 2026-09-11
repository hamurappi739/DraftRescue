# ADR 0003: No general keystroke stream

**Status:** Accepted for Phase 0

## Decision

Do not create a reusable global keystroke-history abstraction in the architecture.

## Why

Such an abstraction would naturally encourage keylogger-like behavior and create a high-risk plaintext stream even if persistence is initially disabled.

## Consequences

Future InputObservation must be designed around supported editable fields/contexts and the minimum information necessary for draft recovery. Any lower-level input mechanism requires separate security justification and explicit approval.
