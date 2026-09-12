# Phase 4 desktop composition and status review — 2026-09-12

## Decision

The desktop composition block is **Pass** for implementation and regression evidence. It wires the protected Phase 4 runtime into application startup, performs metadata-only retention cleanup before the window is exposed, and surfaces a safe localized warning when protected storage is unavailable.

The overall Phase 4 exit remains **Inconclusive** because the target-host certifications are still pending. This review does not promote the phase or enable Preview, Copy, Restore, browser capture, or any content-reveal workflow.

## Evidence

- Debug solution build: 0 warnings, 0 errors.
- Full test suite: **167/167** passed.
- Startup integration tests verify database creation, startup cleanup, runtime disposal, and typed corruption handling.
- View-model tests verify Ready/Unavailable/Incompatible/Corrupt status mapping in Russian and English.
- The public desktop shell has no body-decryption path and no content-bearing diagnostic output.

## Runtime behavior

1. `App` starts the per-user storage runtime before creating the main window.
2. Startup retention cleanup completes before a recovery surface could be exposed.
3. Low-frequency retention runs while the process is alive and is disposed on application exit.
4. Storage failures are classified as `Unavailable`, `Incompatible`, or `Corrupt`; no database replacement or plaintext fallback occurs.
5. The UI displays only generic, localized status copy. Provider exception text and draft contents are never rendered.

## Privacy traceability

This block preserves P-008 (fail-closed uncertainty), P-018 (bounded retention), P-030 (no sensitive logging), P-041 (metadata-only recovery surface), P-043 (protected persistence boundary), P-045 (corruption safety), and P-046 (typed operational diagnostics). The relevant test additions are limited to typed status behavior and do not introduce target-content fixtures.

## Remaining blockers

- Positive DPAPI `CurrentUser` roundtrip on a prepared Windows target profile.
- Real controlled disk-full evidence from a disposable quota/virtual-disk fixture.

Until both are supplied, `scripts/run_phase4_exit_gate.ps1` must remain `Inconclusive`. The next authorized phase is still Phase 5 only after that gate reports `Pass`.
