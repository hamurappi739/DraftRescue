# Phase 4 Privacy / Correctness Traceability

| Concern | Design control | Primary tests |
|---|---|---|
| plaintext written before encryption | protector-before-repository type boundary | PST-001, PST-004, PST-010, PST-020, P4F-001 |
| unencrypted fallback on DPAPI failure | zero repository calls after Protect failure | PST-004, P4F-001 |
| cross-user/machine-wide protection scope | DPAPI `CurrentUser` only | PST-011 |
| low-entropy fingerprint guessing | random 256-bit HMAC key protected separately | PST-009, PST-012, PST-013 |
| revision/history creation | one row per DraftId, monotonic update | PST-002, P4F-016 |
| stale async overwrite | persisted SnapshotSequence compare in transaction | TRK-005, PST-018, P4F-009 |
| startup mass decryption | metadata-only list query | PST-003, PST-014, P4F-018 |
| expired rows visible after cleanup failure | query/action-time expiry check | PST-007, P4F-015 |
| corruption extracts text | no salvage, local quarantine only | PST-019, P4F-011 |
| schema mismatch destroys recovery | explicit user_version + no auto-drop | PST-005, P4F-012, P4F-013 |
| physical-delete overclaim | secure-delete limitations documented | review gate |
| rollback/journal leaves plaintext | ciphertext-only repository + canary scan | PST-015, PST-020, P4F-017, P4F-020 |
| store lock causes unsafe fallback | bounded busy timeout; fail closed | P4F-006 |
| disk full causes temp plaintext | no plaintext temp fallback | P4F-014 |
