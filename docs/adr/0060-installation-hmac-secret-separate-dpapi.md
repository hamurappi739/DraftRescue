# ADR 0060 — Separate DPAPI-protected installation HMAC secret

**Decision:** keyed context fingerprints use a random 256-bit per-installation secret stored separately from the database and protected with DPAPI CurrentUser.

**Reason:** prevent easy offline guessing of low-entropy metadata and avoid coupling fingerprint identity to draft ciphertext.
