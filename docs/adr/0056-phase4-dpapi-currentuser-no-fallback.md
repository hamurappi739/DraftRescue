# ADR 0056 — DPAPI CurrentUser with no unencrypted fallback

**Decision:** Phase 4 protection v1 uses Windows DPAPI `CurrentUser`; protection failure means no durable write. `LocalMachine` and plaintext fallback are forbidden.

**Reason:** preserve per-user privacy boundary and simple MVP key management.
