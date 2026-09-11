# ADR 0055 — Protector-before-repository boundary

**Decision:** repository APIs accept only protected payload records. Plaintext snapshot/payload types cannot cross the repository boundary.

**Reason:** make plaintext-at-rest leakage structurally difficult and ensure DPAPI failure cannot accidentally fall through to storage.

**Consequence:** orchestration protects first; repository never invokes the protector itself.
