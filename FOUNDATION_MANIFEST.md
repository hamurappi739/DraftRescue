# Foundation Manifest

- Project: DraftRescue
- Package role: Phase 0 architecture + deep pre-implementation / future Cursor handoff foundation
- Generated/updated: 2026-09-08
- File count: 1337
- Markdown documentation files: 290
- Accepted ADRs: 63
- Canonical registered test IDs: 229
- Privacy invariants: P-001..P-050
- Correctness invariants: C-001..C-050
- Runtime implementation status: Phase 0 verified; Phase 1 gate passed 8/8 via bounded interactive WPF confirmation; Phase 2 WP-2.1..WP-2.8 security/capability gate is closed; Phase 3 WP-3.1..WP-3.8 application contracts, bounded certified TextPattern/ValuePattern readers, snapshots, current-state tracker, integrated non-leakage evidence and 30-minute operational soak are implemented/tested with exit Pass. Phase 4 WP4.1 contracts/schema, WP4.2 DPAPI CurrentUser/installation-secret boundaries, WP4.3 SQLite bootstrap/serialized repository, WP4.4 protect-before-repository coordinator/metadata-only retention, WP4.6 typed recovery/quarantine/migration safety, and WP4.7 plaintext-at-rest/fault certification are implemented/tested; WP4.8 exit remains pending.
- Current workflow: no Cursor yet; prepare canonical decisions/specifications/tests first
- Build status: Phase 0, Phase 1, Phase 2 and the Phase-3 application/integrated gates are green on Windows x64 with .NET SDK 8.0.424; latest full solution Phase-4 WP4.8 review build 0 warnings/0 errors, tests 148/148, plaintext/fault guard passes; process-kill rollback passes, exit remains Inconclusive pending DPAPI and real disk-full evidence
- Latest deep-design pass: Phase 4 encrypted-persistence package — protector-before-repository boundary, DPAPI CurrentUser payload v1, DPAPI-protected installation HMAC secret, SQLite schema v1, DELETE/EXTRA/secure_delete policy, serialized monotonic writer, metadata-only listing, expiry execution, corruption/quarantine, migration safety, plaintext-at-rest canary certification and Phase 4 exit gate

## Start here now

1. `docs/CURRENT_PROJECT_MODE.md`
2. `docs/DRAFTRESCUE_MASTER_CONTEXT.md`
3. `AGENTS.md`
4. `docs/INDEX.md`
5. `docs/IMPLEMENTATION_READINESS_CHECKLIST.md`

## Phase 1 future implementation start

1. `docs/PHASE1_PREIMPLEMENTATION_PACKAGE_INDEX.md`
2. `docs/PHASE1_IMPLEMENTATION_BOUNDARY.md`
3. `docs/PHASE1_EXIT_CRITERIA.md`

## Phase 2 future implementation start

1. `docs/PHASE2_PREIMPLEMENTATION_PACKAGE_INDEX.md`
2. `docs/PHASE2_IMPLEMENTATION_BOUNDARY.md`
3. `docs/PHASE2_EXIT_CRITERIA.md`

## Phase 3 future implementation start

1. `docs/PHASE3_PREIMPLEMENTATION_PACKAGE_INDEX.md`
2. `docs/PHASE3_IMPLEMENTATION_BOUNDARY.md`
3. `docs/PHASE3_EXIT_CRITERIA.md`


## Phase 4 future implementation start

1. `docs/PHASE4_PREIMPLEMENTATION_PACKAGE_INDEX.md`
2. `docs/PHASE4_IMPLEMENTATION_BOUNDARY.md`
3. `docs/PHASE4_EXIT_CRITERIA.md`

## Canonical Phase 4 persistence decisions

- Repository APIs accept protected records only; plaintext protection completes before persistence.
- Windows protection v1 uses DPAPI `CurrentUser`; no `LocalMachine` or plaintext fallback.
- Fingerprints use a separate random 256-bit HMAC key protected with DPAPI.
- SQLite baseline is `journal_mode=DELETE`, `secure_delete=ON`, `synchronous=EXTRA`, bounded busy timeout and one logical writer.
- One current row per `DraftId`; `SnapshotSequence` is checked transactionally and no revision table exists.
- Recoverable-list/startup/expiry queries are metadata-only and do not decrypt bodies.
- Corrupt/incompatible storage has no salvage/upload/auto-drop path.
- `secure_delete` is defense-in-depth only; no forensic erasure claim.
- Plaintext-at-rest canary certification is a Phase-4 release gate.

## Phase 4 WP4.3 evidence

- `docs/PHASE4_SQLITE_REVIEW_2026-09-06.md`
- `artifacts/phase4-sqlite-gate/PHASE4-SQLITE-GATE.json` — Pass, 133/133 tests, transactional sequence checks and metadata-only listing.
- Runtime coordinator/retention/restore remain intentionally disabled; next bounded package is WP4.4.

## Canonical Phase 3 security/correctness decisions

- Target content is inaccessible until a Phase-2 one-read capability is atomically claimed.
- `TextPatternRange.GetText(-1)` is forbidden in production; bounded document reads use `configuredLimit + 1`.
- `ValuePattern.Value` is only for certified bounded single-line surfaces.
- Oversized content is a typed failure, never a silently truncated draft.
- Generic capture preserves exact user text; no trim/Unicode/newline semantic normalization.
- Phase 3 stores one current in-memory snapshot only; no persistence or revision history.
- A single empty read is not terminal clear evidence.
- Plaintext is copy-minimized and transient; managed-string secure erasure is not falsely claimed.

## Phase 4 WP4.4 evidence

- `docs/PHASE4_COORDINATOR_REVIEW_2026-09-07.md`
- `artifacts/phase4-coordinator-gate/PHASE4-COORDINATOR-GATE.json` — Pass, 139/139 tests, protect-before-repository and metadata-only retention.
- Coordinator/retention are implemented without Preview, Copy or Restore; WP4.6 recovery/quarantine and migration handling is now closed.

## Phase 4 WP4.6 evidence

- `docs/PHASE4_RECOVERY_REVIEW_2026-09-08.md`
- `artifacts/phase4-recovery-gate/PHASE4-RECOVERY-GATE.json` — Pass, 142/142 tests, local-only quarantine, typed corruption, no salvage or auto-drop.
- WP4.7 plaintext-at-rest canary and crash/fault certification is closed; WP4.8 final Phase 4 exit remains pending.

## Phase 4 WP4.7 evidence

- `docs/PHASE4_FAULT_REVIEW_2026-09-08.md`
- `artifacts/phase4-fault-gate/PHASE4-FAULT-GATE.json` — Pass, 148/148 tests, zero UTF-8/UTF-16 canary matches and bounded fault outcomes.
- WP4.8 final Phase 4 exit review is recorded Inconclusive; Preview, Copy and Restore stay disabled pending blocker resolution.

## Phase 4 WP4.8 evidence

- `docs/PHASE4_FINAL_EXIT_REVIEW_2026-09-08.md`
- `artifacts/phase4-exit-gate/PHASE4-EXIT-GATE.json` — Inconclusive, prior gates present, source boundary Pass, 148/148 tests.
- Blockers are explicit: successful DPAPI CurrentUser roundtrip on the target profile and OS-level kill/real disk-full certification. No plaintext fallback or unsafe recovery path was introduced.

## Phase 4 WP4.1 evidence

- `docs/PHASE4_CONTRACTS_REVIEW_2026-09-06.md`
- `artifacts/phase4-contracts-gate/PHASE4-CONTRACTS-GATE.json` — Pass, 120/120 tests, zero plaintext repository/schema violations.
- Runtime persistence is intentionally disabled; WP4.2 DPAPI/installation-secret gate passes with fail-closed profile-unavailable evidence; next bounded package is WP4.3 SQLite bootstrap/schema migration and serialized writer.

- `docs/PHASE4_DPAPI_REVIEW_2026-09-06.md`
- `artifacts/phase4-dpapi-gate/PHASE4-DPAPI-GATE.json` — Pass, 127/127 tests, CurrentUser-only guard; test host DPAPI profile probe is unavailable and fail-closed.
- Next bounded package is WP4.3 SQLite bootstrap/schema migration and serialized writer.

## Static verification status

The package is expected to pass JSON/XML/reference/catalog/ADR/traceability/scope/ZIP checks. A real Windows `.NET 8` restore/build/test remains a separate future execution gate.

---

## Codex takeover package — 2026-09-02

The full foundation was repackaged for transition to Codex.

Start with `CODEX_START_HERE.md`.

Codex-specific takeover artifacts:

- `CODEX_START_HERE.md`
- `CODEX_PROMPT_TO_PASTE.txt`
- `ORIGINAL_AI_MAXIMUM_WORK_PROMPT_2026-09-10.md`
- `CODEX_HANDOFF_STATUS.json`
- `CODEX_HANDOFF_MANIFEST.md`
- `docs/CODEX_EXECUTION_GUIDE.md`
- `docs/CODEX_REMAINING_WORK_AND_TEST_PLAN.md`

Critical status: Phase 0 is verified; Phase 1 WP-1.1 foreground and WP-1.2 focused UIA metadata prototypes are implemented. Target-content reads, persistence and restore remain unimplemented. Phases 2–4 have deep pre-implementation specifications, and Phases 5–10 remain implementation work.

Current Codex handoff file count: **575 files**.


## Codex audit resolution

- `CODEX_FOUNDATION_AUDIT_2026-09-02.md` is preserved in the package.
- `CODEX_AUDIT_RESOLUTION_2026-09-02.md` records the disposition.
- F-001 resolved: PowerShell verification now checks native exit codes.
- F-002 resolved: real Windows restore/build/test passed with SDK 8.0.424 and Avalonia shell process launched.
- F-003 resolved: inventory/checksum/manifest are regenerated together.
- F-004 addressed with a stable .NET 8 SDK-family policy in `global.json` and `docs/SDK_REPRODUCIBILITY_POLICY.md`; first-green SDK version is 8.0.424.

Checksum-list rule: `FOUNDATION_SHA256SUMS.txt` excludes only itself; all other regular package files are checksummed.
