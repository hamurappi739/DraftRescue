# Phase 4 WP4.7 Plaintext/Fault Review — 2026-09-08

## Decision

WP4.7 bounded plaintext-at-rest and fault-certification package is **Pass**. The gate uses synthetic canaries only and keeps Phase 4 exit pending until the dedicated WP4.8 exit review.

## Implemented scope

- Repeated protected checkpoints are scanned bytewise for UTF-8 and UTF-16 canaries after the repository is closed; no canary is present in DraftRescue-owned database artifacts.
- A 500-checkpoint stress test confirms one logical current-state row and the highest snapshot sequence.
- A transaction rollback test confirms the previous complete row remains after a pre-commit failure.
- Store-busy and disk-full-equivalent path failures remain typed/unavailable and never create a plaintext fallback.
- The plaintext/fault boundary guard rejects file-based plaintext writes, clipboard/network shortcuts, WAL, and broad decrypted enumeration in persistence/protection code.

## Evidence

- Gate: `artifacts/phase4-fault-gate/PHASE4-FAULT-GATE.json` — Pass.
- Boundary guard: `artifacts/phase4-fault-gate/PHASE4-PLAINTEXT-CANARY-GUARD.json` — Pass.
- Full test suite: 148/148; build: 0 warnings, 0 errors.

## Invariants covered

P-002, P-003, P-004, P-005, P-006, P-017, P-025, P-028, P-030, P-031, P-032, P-033, P-035, P-036, P-037; C-019, C-020, C-026, C-027, C-028, C-029, C-030, C-031, C-032, C-033.

## Remaining boundary

Phase 4 exit remains pending. WP4.8 must combine the accepted architecture, protection, SQLite, recovery, canary, and fault evidence into the final Phase-4 exit gate and then stop before Phase 5 Preview/Copy/Restore workflows.
