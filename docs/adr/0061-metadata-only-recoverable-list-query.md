# ADR 0061 — Recoverable-list query never loads/decrypts bodies

**Decision:** the normal recovery list selects presentation metadata only; it does not select `protected_payload` and does not call Unprotect.

**Reason:** minimize plaintext/ciphertext exposure and preserve the explicit Preview content-reveal model.
