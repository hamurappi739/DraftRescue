# Roadmap Execution

Canonical roadmap:

- Phase 0 — Architecture
- Phase 1 — Active App + Field Detection
- Phase 2 — Secure Field Guard
- Phase 3 — Draft Tracking Prototype
- Phase 4 — Encrypted Local Persistence
- Phase 5 — Recovery UI
- Phase 6 — Restore
- Phase 7 — Browser Support
- Phase 8 — Electron Apps
- Phase 9 — App Profiles
- Phase 10 — Hardening & Privacy Tests

## Current gate: Phase 0

Phase 0 is complete only when:

- solution restores successfully;
- entire solution builds;
- tests pass;
- Avalonia shell launches on Windows x64;
- project dependencies match the documented direction;
- no capture/observation/persistence functionality exists;
- privacy rules are documented and reflected in contracts;
- `AGENTS.md` is present for future coding-agent work.

## Phase 1 entry gate

Do not start Phase 1 until Phase 0 has been verified on Windows with the .NET 8 SDK.

Phase 1 must remain detection-only. It should establish active application and supported field detection without draft persistence, without keyboard hooks, and without attempting to solve all applications at once.
