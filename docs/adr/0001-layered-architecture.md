# ADR 0001: Layered architecture with explicit platform boundary

**Status:** Accepted for Phase 0

## Decision

Use Domain, Application, Platform.Windows, Infrastructure, Desktop, and Tests projects with one-way dependencies.

## Why

DraftRescue needs strong separation between privacy/security policy and dangerous OS-specific capabilities. Keeping Windows observation behind a platform boundary allows policy and tests to remain independent from Win32/UI Automation.

## Consequences

- More projects than a single-project prototype.
- Clearer review boundary for future sensitive code.
- Core logic can be tested without launching Avalonia or Windows adapters.
