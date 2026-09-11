# ADR 0014 — Background Tray Lifecycle

**Status:** Accepted

## Decision

DraftRescue runs as a background utility with a tray icon. Closing the main window does not quit. Explicit Quit stops protection and shows a warning. Start-with-Windows is a visible user setting and is recommended On after normal installation/first launch.

## Consequences

- main window lifetime is decoupled from process lifetime;
- no Pause mode in MVP;
- startup implementation mechanism remains a packaging decision.
