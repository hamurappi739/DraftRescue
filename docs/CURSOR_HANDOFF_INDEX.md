# Cursor Handoff Index

**Purpose:** when Cursor is eventually introduced, this is the mandatory reading order before it changes code.

Cursor must not treat one document as optional context and improvise the rest.

## 1. Product source of truth

1. `DRAFTRESCUE_MASTER_CONTEXT.md`
2. `../AGENTS.md`

If they conflict with later implementation details, product/privacy invariants win unless the user explicitly changes them.

## 2. Architecture

3. `ARCHITECTURE.md`
4. `MODULE_CONTRACTS.md`
5. `OBSERVATION_AND_GATING_PIPELINE.md`
6. `FUTURE_CONTRACT_CATALOG.md`
7. `OPEN_DECISIONS.md`
8. `adr/`

## 3. Privacy/security

9. `PRIVACY_SECURITY.md`
10. `SECURITY_CLASSIFICATION_SPEC.md`
11. `THREAT_MODEL.md`
12. `SECURITY_ABUSE_CASES.md`
13. `DIAGNOSTICS_AND_SUPPORT_SPEC.md`
14. `BROWSER_PRIVACY_SPEC.md` when browser work begins.

## 4. Draft semantics

15. `DRAFT_LIFECYCLE_STATE_MACHINE.md`
16. `SUBMISSION_AND_CLEAR_DETECTION.md`
17. `RETENTION_POLICY_SPEC.md`
18. `CRASH_AND_PERSISTENCE_SEMANTICS.md`
19. `DATA_MODEL_PROPOSAL.md`

## 5. Recovery/restore

20. `RECOVERY_MATCHING_SPEC.md`
21. `RESTORE_SAFETY_SPEC.md`
22. `RECOVERY_UX_SPEC.md`
23. `PRODUCT_BEHAVIOR_MATRIX.md`

## 6. UI

24. `UI_UX_MASTER_SPEC.md`
25. `UI_STATE_MODEL.md`
26. `UI_COPY_BASELINE.md`
27. `CONFIGURATION_SPEC.md`

## 7. Execution/testing

28. `WINDOWS_OBSERVATION_RESEARCH_PLAN.md` for Phase 1.
29. `PERFORMANCE_RESOURCE_BUDGETS.md`
30. `TEST_STRATEGY_MASTER.md`
31. `PRIVACY_TEST_MATRIX.md`
32. `PHASE_ACCEPTANCE_GATES.md`
33. `PHASE_BREAKDOWN.md`
34. `ROADMAP_EXECUTION.md`

## 7A. Phase 1 mandatory package

Before any Phase 1 implementation, read `PHASE1_PREIMPLEMENTATION_PACKAGE_INDEX.md` and every document/spec it lists. Phase 1 has a hard zero-target-content-read boundary.

## 8. Required behavior for every Cursor task

Before editing, Cursor should state:

- current phase/subtask;
- documents relevant to that subtask;
- exact files it expects to modify;
- privacy invariants that apply;
- explicit out-of-scope items.

After editing, Cursor must report:

- files changed;
- build/test commands and results;
- new assumptions;
- unresolved issue(s);
- confirmation that it did not continue into the next phase.

## 9. No silent architecture invention

If implementation requires an unresolved choice from `OPEN_DECISIONS.md`, Cursor must stop at the smallest safe boundary and surface the decision rather than choosing a broad architecture on its own.
## Phase 2 mandatory package

Before any SecureInputGuard implementation, read `PHASE2_PREIMPLEMENTATION_PACKAGE_INDEX.md` and every mandatory document listed there. Phase 2 must end with capability issuance/validation and still contain zero real target-content reads.

## Phase 3 mandatory package

Before any target-content reading implementation, read `PHASE3_PREIMPLEMENTATION_PACKAGE_INDEX.md` and every mandatory document listed there. Phase 3 may consume text only through one-read capabilities, keeps only one transient in-memory current snapshot, and must stop before durable persistence.


## Phase 4 implementation entry

When Phase 4 is eventually activated, read in this order:

1. `PHASE4_PREIMPLEMENTATION_PACKAGE_INDEX.md`
2. `PHASE4_IMPLEMENTATION_BOUNDARY.md`
3. `PROTECTED_RECORD_MODEL_V1.md`
4. `DPAPI_PAYLOAD_FORMAT_V1.md`
5. `SQLITE_SCHEMA_V1_SPEC.md`
6. `SQLITE_CONNECTION_PRAGMAS_SPEC.md`
7. `PERSISTENCE_COORDINATOR_SPEC.md`
8. `PHASE4_ARCHITECTURE_GUARDS.md`
9. `PHASE4_FAULT_INJECTION_MATRIX.md`
10. `PHASE4_EXIT_CRITERIA.md`

Implement one `WP-4.*` at a time and stop after its tests.
