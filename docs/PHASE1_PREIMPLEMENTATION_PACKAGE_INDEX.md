# Phase 1 Pre-Implementation Package Index

**Purpose:** mandatory reading order before any Phase 1 code is written.

## Canonical gate

Phase 1 is `Active App + Field Detection`, but in DraftRescue that means **content-free observation and candidate metadata only**. It does not include text reading.

## Read in this order

1. `PHASE1_IMPLEMENTATION_BOUNDARY.md`
2. `PHASE1_WINDOWS_EXPERIMENT_PLAN.md`
3. `PHASE1_WINDOWS_API_VERIFICATION.md`
4. `WIN_EVENT_ROUTING_SPEC.md`
5. `WINDOWS_PLATFORM_THREADING_SPEC.md`
6. `UIA_PROPERTY_SAFETY_CLASSIFICATION.md`
7. `UIA_CACHE_AND_PROPERTY_BUDGET_SPEC.md`
8. `CANDIDATE_METADATA_NORMALIZATION_SPEC.md`
9. `FOCUS_EVENT_DEDUPLICATION_SPEC.md`
10. `SECURITY_CLASSIFIER_DECISION_TREE.md` (future boundary awareness; do not implement Phase 2 early)
11. `FAULT_INJECTION_PLAN.md`
12. `PHASE1_FAULT_AND_PERFORMANCE_MATRIX.md`
13. `NOTEPAD_PHASE1_EXPERIMENT_PLAN.md`
14. `PRIVACY_NEGATIVE_TEST_PROTOCOL.md`
15. `EXPERIMENT_RESULT_RECORD_FORMAT.md`
16. `PHASE1_EXIT_CRITERIA.md`

## Machine-readable inputs

- `../specs/candidate-metadata.schema.json`
- `../specs/uia-property-policy.v1.json`
- `../specs/examples/candidate-metadata.synthetic.json`
- `../specs/win-event-routing-fixtures.v1.json`
- `../specs/phase1-test-matrix.v1.json`
- `../specs/experiment-result.schema.json`
- `../specs/examples/experiment-result.synthetic.json`
- `../specs/security-classification-fixtures.v1.json` (Phase 2 preview/contract evidence)

## Accepted ADRs specific to this package

- ADR 0005 — event-driven observation, no keyboard hook
- ADR 0012 — context-generation race control
- ADR 0016 — dedicated UIA MTA worker
- ADR 0031 — content-free/non-blocking event callbacks
- ADR 0032 — tiered UIA property acquisition
- ADR 0033 — bounded coalescing/latest-context convergence
- ADR 0034 — editable does not mean allowed
- ADR 0035 — Phase 1 zero target-content reads
- ADR 0036 — UIA cache allowlist
- ADR 0037 — browser forms not generic early target
- ADR 0038 — Notepad reconnaissance before certification

## Primary invariants

Privacy: P-001, P-002, P-003, P-008, P-017, P-021, P-025, P-026, P-028, P-029, P-030.

Correctness: C-003, C-016, C-017, C-018, C-021, C-022, C-023, C-025.

## Stop condition

Do not begin Phase 2 merely because Phase 1 code compiles. The experiment records, fault tests, zero-content-read proof, soak data, and exit checklist must be reviewed first.
