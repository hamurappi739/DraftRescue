# ADR 0018 — No privileged helper or automatic elevation in MVP

**Status:** Accepted

## Decision

DraftRescue runs at normal user integrity and does not auto-elevate, inject helpers, or install a privileged service to reach elevated targets.

## Consequences

- some elevated applications are unsupported for direct capture/restore;
- unsupported privilege boundaries fail closed;
- architecture remains materially easier to audit for keylogger-like behavior.
