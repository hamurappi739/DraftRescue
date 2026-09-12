# Phase 4 protected startup review — 2026-09-12

## Decision

The desktop persistence composition now enforces the protection boundary at startup. A runtime is reported as `Ready` only after the configured installation-secret provider succeeds and the protected SQLite repository, retention coordinator, and checkpoint coordinator are composed together.

## Startup order

1. Resolve the per-user data paths.
2. Load or create the DPAPI-backed installation secret.
3. Open and validate the SQLite schema and connection policy.
4. Compose the protect-before-repository `PersistenceCheckpointCoordinator`.
5. Run metadata-only expiry cleanup.
6. Start low-frequency retention cleanup.
7. Expose the runtime to the desktop shell.

If steps 2–4 fail, the partial runtime is disposed, the database is not created as a replacement, and the UI receives a generic `Unavailable` state. There is no plaintext fallback and no provider exception text is surfaced.

## Evidence

- Full solution build: 0 warnings, 0 errors.
- Full test suite: **180/180** passed.
- Startup tests cover successful composition, corrupt-store preservation, and a failed protection store that stops before database creation.
- Existing DPAPI, secret-store, SQLite, retention, recovery, and plaintext-at-rest tests remain green.

## Privacy traceability

The block preserves P-008, P-030, P-043, P-045, and P-046: uncertain protection fails closed, no sensitive diagnostics are emitted, protection precedes persistence, corrupt stores are not replaced, and operational outcomes remain typed. Phase 5 decryption and restore workflows remain out of scope.
