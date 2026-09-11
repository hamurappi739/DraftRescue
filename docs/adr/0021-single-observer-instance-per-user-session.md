# ADR 0021 — Single observer instance per interactive user session

**Status:** Accepted

## Decision

DraftRescue enforces one active observation/tracking/repository-writer instance per interactive user session. Secondary launches signal the existing instance with content-free commands and exit.

## Consequences

- prevents duplicated observations/checkpoints/notifications;
- local IPC primitive remains packaging-dependent;
- IPC is not a draft-content transport in MVP.
