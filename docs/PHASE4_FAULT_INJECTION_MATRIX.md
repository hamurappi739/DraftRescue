# Phase 4 Fault Injection and Negative Matrix

| ID | Injection | Required result |
|---|---|---|
| P4F-001 | DPAPI Protect throws | no repository call; plaintext not logged |
| P4F-002 | DPAPI Unprotect throws in test retrieval | typed unreadable record; no fallback |
| P4F-003 | secret file missing while DB has rows | identity unavailable; no silent new key |
| P4F-004 | secret file corrupt | fail closed; no fingerprint guessing |
| P4F-005 | SQLite open fails | storage unavailable; RAM tracking may continue bounded |
| P4F-006 | DB locked beyond busy timeout | bounded typed failure; no infinite retry |
| P4F-007 | crash before transaction commit | old complete row or no inserted row |
| P4F-008 | crash after commit | new complete row |
| P4F-009 | stale encrypted sequence arrives late | `StaleIgnored` |
| P4F-010 | equal sequence with contradictory record | typed conflict; no overwrite |
| P4F-011 | corrupted DB header/page | quarantine/fail-closed path; no salvage |
| P4F-012 | unknown `user_version` | incompatible store; no auto-drop |
| P4F-013 | migration throws mid-transaction | original schema/data remains usable where SQLite transaction guarantees permit |
| P4F-014 | disk full during checkpoint | old committed row remains; no plaintext temp fallback |
| P4F-015 | expiry delete fails | expired card hidden; bounded cleanup retry |
| P4F-016 | 500 edits/checkpoints | exactly one logical row per DraftId |
| P4F-017 | canary scan after repeated updates | zero plaintext canary in DraftRescue-owned durable files |
| P4F-018 | metadata list with 100 rows | Unprotect invocation count = 0 |
| P4F-019 | discard all | no decrypt calls |
| P4F-020 | app kill during rollback-journal commit | SQLite recovers to valid old/new transaction state |
