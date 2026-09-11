# ADR 0016 — Dedicated MTA Worker for UI Automation

**Status:** Accepted

## Decision

Desktop-wide UI Automation client operations run in a dedicated non-Avalonia UI worker using COM MTA semantics. Raw UIA elements remain platform-local and short-lived.

## Why

Microsoft UI Automation guidance warns that desktop-wide UIA calls on an application's UI thread can cause severe responsiveness problems and recommends a separate MTA thread for such clients.

## Consequences

- Platform.Windows owns UIA subscription/thread lifetime;
- application/UI communicate through sanitized async contracts;
- event registration/removal is not scattered across arbitrary thread-pool work;
- context-generation checks reject stale completions.
