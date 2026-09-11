# Codex Handoff Manifest

This ZIP is the complete DraftRescue foundation plus Codex-specific takeover material.

## Codex-specific files added at handoff

- `CODEX_START_HERE.md` — primary takeover document.
- `CODEX_PROMPT_TO_PASTE.txt` — ready-to-paste initial Codex instruction.
- `ORIGINAL_AI_MAXIMUM_WORK_PROMPT_2026-09-10.md` — large copy-paste continuation prompt for the original project-generating AI, focused on WP4.8 blocker resolution.
- `CODEX_HANDOFF_STATUS.json` — machine-readable project status.
- `CODEX_HANDOFF_MANIFEST.md` — this file.
- `docs/CODEX_EXECUTION_GUIDE.md` — per-work-package execution protocol.
- `docs/CODEX_REMAINING_WORK_AND_TEST_PLAN.md` — full remaining implementation/test plan.
- `CODEX_REVIEW_TO_ORIGINAL_AI.md` — current implementation review and bounded continuation brief.
- `docs/PHASE1_WP14_EVIDENCE_2026-09-03.md` — content-safe WP-1.4 evidence summary.
- `docs/PHASE1_PROVIDER_STRESS_REVIEW_2026-09-04.md` — formal provider-stress review and the remaining fail-closed WPF gate decision.
- `scripts/phase1_evidence_package_guard.ps1` — fail-closed integrity/privacy guard for the complete Phase-1 evidence package.
- `scripts/run_wpf_interactive_preflight.ps1` — content-free readiness probe for the interactive WPF confirmation session.
- `scripts/run_wpf_interactive_review.ps1` — unified preflight + bounded interactive review runner with a single fail-closed JSON record.
- `docs/PHASE1_WPF_INTERACTIVE_RUNBOOK.md` — exact bounded procedure for the remaining interactive WPF gate, including privacy limits and result interpretation.
- `scripts/phase1_harness_process_guard.ps1` — isolated lifecycle guard for direct Release harness execution and child-process cleanup.
- `scripts/run_phase2_security_gate.ps1` — metadata-only Phase 2 matrix runner and source/API boundary report.
- `docs/PHASE2_SECURITY_GATE_REVIEW_2026-09-05.md` — WP-2.1..WP-2.6 implementation and evidence review.
- `scripts/phase3_content_boundary_guard.ps1` — fail-closed Phase 3 source/API boundary guard.
- `scripts/run_phase3_application_gate.ps1` — WP-3.1/WP-3.2/WP-3.3/WP-3.4..WP-3.6 application/reader gate runner.
- `scripts/run_phase3_integrated_gate.ps1` — WP-3.7 integrated fault/non-leakage gate runner.
- `scripts/run_phase3_operational_soak.ps1` — WP-3.8 30-minute synthetic operational soak runner.
- `scripts/run_phase3_exit_gate.ps1` — final Phase 3 exit gate combiner.
- `docs/PHASE3_APPLICATION_GATE_REVIEW_2026-09-05.md` — bounded Phase 3 application semantics review.
- `docs/PHASE3_INTEGRATED_GATE_REVIEW_2026-09-06.md` — integrated Phase 3 privacy/fault evidence review.
- `docs/PHASE3_FINAL_EXIT_REVIEW_2026-09-06.md` — final Phase 3 exit decision and boundary review.
- `docs/UI_UX_CODEX_PROPOSAL_2026-09-03.md` — понятный Apple-подобный UI/UX handoff без изменения privacy/phase boundaries.
- `docs/BRAND_AND_APPEARANCE_PROPOSAL_2026-09-03.md` — rationale логотипа, цветовые правила, язык и Light/Dark appearance.
- `docs/PHASE4_CONTRACTS_REVIEW_2026-09-06.md` — WP4.1 protected-record/canonical-schema review and evidence.
- `scripts/phase4_contracts_guard.ps1` — ciphertext-only repository and SQLite schema boundary guard.
- `scripts/run_phase4_contracts_gate.ps1` — WP4.1 build/test/guard gate runner.
- `docs/PHASE4_DPAPI_REVIEW_2026-09-06.md` — WP4.2 DPAPI and installation-secret review.
- `scripts/phase4_dpapi_guard.ps1` — CurrentUser-only DPAPI/fallback boundary guard.
- `scripts/run_phase4_dpapi_gate.ps1` — WP4.2 build/test/guard gate runner.
- `docs/PHASE4_SQLITE_REVIEW_2026-09-06.md` — WP4.3 SQLite runtime/repository review.
- `scripts/phase4_sqlite_guard.ps1` — schema/transaction/privacy boundary guard.
- `scripts/run_phase4_sqlite_gate.ps1` — WP4.3 build/test/guard gate runner.
- `docs/PHASE4_COORDINATOR_REVIEW_2026-09-07.md` — WP4.4 coordinator/retention review.
- `scripts/phase4_coordinator_guard.ps1` — coordinator privacy boundary guard.
- `scripts/run_phase4_coordinator_gate.ps1` — WP4.4 build/test/guard gate runner.
- `docs/PHASE4_RECOVERY_REVIEW_2026-09-08.md` — WP4.6 corruption/quarantine/migration review.
- `scripts/phase4_recovery_guard.ps1` — WP4.6 fail-closed recovery boundary guard.
- `scripts/run_phase4_recovery_gate.ps1` — WP4.6 build/test/guard gate runner.
- `docs/PHASE4_FAULT_REVIEW_2026-09-08.md` — WP4.7 plaintext-at-rest and fault-certification review.
- `scripts/phase4_plaintext_canary_guard.ps1` — WP4.7 plaintext/fallback boundary guard.
- `scripts/run_phase4_fault_gate.ps1` — WP4.7 build/test/guard gate runner.
- `docs/PHASE4_FINAL_EXIT_REVIEW_2026-09-08.md` — WP4.8 combined exit review and explicit blockers.
- `scripts/run_phase4_exit_gate.ps1` — WP4.8 evidence combiner and fail-closed exit gate.
- `scripts/probe_phase4_dpapi_runtime.ps1` — content-free target-profile DPAPI CurrentUser probe.
- `experiments/DraftRescue.Phase4CrashProbe/` — isolated child-process SQLite rollback probe.
- `scripts/run_phase4_process_kill_probe.ps1` — WP4.8 OS-level process-kill certification runner.
- `scripts/probe_phase4_target_environment.ps1` — content-free profile/volume capability probe for WP4.8.
- `scripts/run_phase4_target_certification.ps1` — unified target-host certification runner for DPAPI, process-kill and externally supplied real disk-full evidence.
- `docs/PHASE4_TARGET_CERTIFICATION_RUNBOOK.md` — safe target-environment procedure and evidence requirements for closing the two WP4.8 blockers.
- `scripts/phase4_artifact_privacy_guard.ps1` — content-free JSON artifact guard for Phase 4 sensitive-field/path leakage and typed evidence flags.
- `scripts/phase4_handoff_consistency_guard.ps1` — machine-readable consistency guard for Phase 4 status, evidence, and package inventory.

All pre-existing DraftRescue files remain included.


## Audit follow-up files

- `CODEX_FOUNDATION_AUDIT_2026-09-02.md` — Codex audit supplied after initial handoff.
- `CODEX_AUDIT_RESOLUTION_2026-09-02.md` — fixes/disposition of the audit findings.
- `docs/SDK_REPRODUCIBILITY_POLICY.md` — .NET 8 SDK resolution and verification policy.

F-002 was resolved by the real Windows x64 restore/build/test and Avalonia shell run recorded in the resolution document.


## Current audited package inventory

- Regular files: **1337**
- Markdown files: **290**
- SHA-256 entries: **1337** (`FOUNDATION_SHA256SUMS.txt` excludes itself)
