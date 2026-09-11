# Phase 3 — Draft Tracking Prototype Pre-Implementation Package

**Status:** canonical pre-implementation package. No Phase 3 production implementation should begin without reading this index.

## Objective

Phase 3 is the first phase allowed to read user-entered target text, but only through the one-read capability issued by Phase 2.

The safety/correctness target is:

> **A freshly claimed `AllowedFieldHandle` may produce at most one bounded, transient, generation-bound `FieldTextSnapshot`. The exact text is not logged, normalized semantically, persisted, copied, or exposed to UI by Phase 3. Stale/oversized/failed reads never become draft state.**

Phase 3 also introduces in-memory current-snapshot tracking for one logical field. It still does **not** implement encrypted durable persistence.

## Mandatory reading order

1. `PHASE3_IMPLEMENTATION_BOUNDARY.md`
2. `CONTENT_READ_PIPELINE_SPEC.md`
3. `ONE_SHOT_TEXT_READER_CONTRACT.md`
4. `FIELD_TEXT_SNAPSHOT_MODEL.md`
5. `TEXT_READ_STRATEGY_MATRIX.md`
6. `TEXT_LENGTH_AND_TRUNCATION_POLICY.md`
7. `PLAINTEXT_MEMORY_LIFETIME_SPEC.md`
8. `TEXT_NORMALIZATION_SPEC.md`
9. `EMPTY_CLEAR_SEMANTICS_PHASE3.md`
10. `CONTENT_READ_RACE_SPEC.md`
11. `IN_MEMORY_DRAFT_TRACKER_SPEC.md`
12. `PHASE3_NONLEAKAGE_ARGUMENT.md`
13. `PHASE3_DI_AND_ARCHITECTURE_TEST_SPEC.md`
14. `PHASE3_FAULT_AND_NEGATIVE_MATRIX.md`
15. `PHASE3_WINDOWS_API_VERIFICATION.md`
16. `PHASE3_EXIT_CRITERIA.md`

Global prerequisites remain Phase 1/2 packages plus privacy/correctness invariant catalogs.

## Phase 3 work packages

- WP-3.1 — one-shot reader contract and typed results;
- WP-3.2 — TextPattern bounded read adapter;
- WP-3.3 — certified ValuePattern bounded-surface adapter;
- WP-3.4 — `FieldTextSnapshot` and generation/race enforcement;
- WP-3.5 — in-memory current-snapshot tracker;
- WP-3.6 — empty/clear stabilization;
- WP-3.7 — plaintext lifetime/architecture/fault tests;
- WP-3.8 — Phase 3 gate review.

## Explicit non-goals

No SQLite, DPAPI, disk draft payloads, recovery list, Preview, Copy, Restore, browser capture support, keyboard hooks, cloud/network, semantic text analysis, revision history, generic LegacyIAccessible fallback, or rich-text formatting capture.
