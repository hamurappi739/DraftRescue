# Phase 2 — SecureInputGuard Pre-Implementation Package

**Status:** canonical pre-implementation package. No Phase 2 production implementation should begin without reading this index.

## Objective

Phase 2 proves one safety property before any draft content reader is introduced:

> **Only a candidate that has passed complete metadata-only, fail-closed security classification may receive a one-read `AllowedFieldHandle`. Every denied, uncertain, stale, unsupported, private, credential, banking, or failed candidate is structurally unable to reach a content reader.**

Phase 2 still reads **zero target text**.

## Mandatory reading order

1. `PHASE2_IMPLEMENTATION_BOUNDARY.md`
2. `SECURITY_SIGNAL_MODEL_V1.md`
3. `SECURITY_CLASSIFIER_DECISION_TREE.md`
4. `SECURITY_POLICY_EVALUATION_SPEC.md`
5. `POSITIVE_ALLOW_PREDICATES_SPEC.md`
6. `ALLOWED_FIELD_HANDLE_CAPABILITY_SPEC.md`
7. `CAPABILITY_REVOCATION_AND_CONSUMPTION_SPEC.md`
8. `PHASE2_NONINTERFERENCE_ARGUMENT.md`
9. `PHASE2_DI_AND_ARCHITECTURE_TEST_SPEC.md`
10. `NATIVE_SECURE_FIELD_FIXTURE_PLAN.md`
11. `PHASE2_FAULT_AND_NEGATIVE_MATRIX.md`
12. `PHASE2_WINDOWS_API_VERIFICATION.md`
13. `PHASE2_EXIT_CRITERIA.md`

Global prerequisites remain:

- `PRIVACY_INVARIANTS_CATALOG.md`
- `CORRECTNESS_INVARIANTS_CATALOG.md`
- `APP_PROFILE_SPEC.md`
- `PROFILE_VERSION_COMPATIBILITY_POLICY.md`
- `UIA_PROPERTY_SAFETY_CLASSIFICATION.md`
- `CREDENTIAL_BANKING_SIGNAL_CATALOG.md`
- `TEST_CASE_CATALOG.md`

## Phase 2 work packages

- WP-2.1 — typed security signal model;
- WP-2.2 — pure deterministic policy evaluator;
- WP-2.3 — native password/protected deny;
- WP-2.4 — provider/unknown/stale/integrity deny;
- WP-2.5 — positive allow predicate for synthetic target;
- WP-2.6 — one-read `AllowedFieldHandle` issuer/validator;
- WP-2.7 — negative/fault harness and architecture proof;
- WP-2.8 — Phase 2 gate review.

## Explicit non-goals

Phase 2 does **not** implement:

- `ValuePattern.Value` reads;
- `TextPattern` reads;
- `LegacyIAccessible` value reads;
- keyboard hooks;
- draft tracking;
- persistence;
- recovery;
- Restore writes;
- browser capture support;
- semantic inspection of typed text.

## Deliverable

At Phase 2 exit, the repository has a security gate and an unforgeable-in-normal-code capability abstraction, but still has no target-content reader implementation. Phase 3 is the first phase allowed to consume that capability.
