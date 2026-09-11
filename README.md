# DraftRescue

**Never lose typed text again.**

DraftRescue is a privacy-first Windows desktop application intended to recover temporary, unsaved drafts from ordinary text fields after an unexpected tab, window, application, or PC restart.

This repository currently contains the **Phase 0 architecture foundation plus the Phase 1 WP-1.1 foreground, WP-1.2 focused metadata and WP-1.3 coordination prototypes, with partial WP-1.4 evidence**. It intentionally does **not** capture keyboard input, read target text through UI Automation, persist drafts, restore text, access the clipboard, or integrate with browsers.

## Non-negotiable privacy boundary

DraftRescue must not become a keylogger.

- No full typing history.
- No passwords, PINs, banking fields, security codes, credentials, or secure input.
- Private/incognito browser contexts are excluded by default.
- No cloud text processing or cloud AI.
- No draft contents in logs.
- Recoverable drafts are temporary, local, encrypted, and retention-limited when persistence is implemented later.
- Uncertain secure/private context fails closed: do not persist.

## Solution layout

- `src/DraftRescue.Domain` — core privacy-safe value objects and lifecycle concepts.
- `src/DraftRescue.Application` — use-case contracts and policy abstractions.
- `src/DraftRescue.Platform.Windows` — future Win32/UI Automation adapters only.
- `src/DraftRescue.Infrastructure` — future local encrypted persistence and infrastructure adapters.
- `src/DraftRescue.Desktop` — Avalonia composition root and UI shell.
- `tests/DraftRescue.Tests` — core/architecture tests that do not require launching the UI.
- `docs` — architecture, threat model, ADRs, execution rules, and canonical project context.

## Phase 0 verification

On a Windows x64 machine with a compatible stable .NET 8 SDK installed (see `global.json` and `docs/SDK_REPRODUCIBILITY_POLICY.md`):

```powershell
./scripts/verify.ps1
```

or:

```cmd
scripts\verify.cmd
```

The scripts must fail on the first failing scope/SDK/restore/build/test step. Do not begin Phase 1 until the complete gate succeeds and the Avalonia shell is launched manually.

## Current status

Phase 0 is verified on Windows x64 with .NET SDK 8.0.424. WP-1.1 provides content-free foreground WinEvent observation, WP-1.2 focused UI Automation metadata, WP-1.3 metadata-only coordination, and WP-1.4 now has synthetic, Notepad and managed WPF metadata-only reconnaissance records plus a corrected 30-minute synthetic soak; target-content reads and all persistence features remain future work. The UI/UX direction is documented in `docs/UI_UX_CODEX_PROPOSAL_2026-09-03.md`. See `CODEX_FOUNDATION_AUDIT_2026-09-02.md`, `CODEX_AUDIT_RESOLUTION_2026-09-02.md`, and `CODEX_REVIEW_TO_ORIGINAL_AI.md`.

## UI/UX design baseline

Future UI implementation should follow:

- `docs/UI_UX_MASTER_SPEC.md` — complete minimal UI direction and wireframes.
- `docs/UI_STATE_MODEL.md` — presentation states and security boundaries for ViewModels.
- `docs/UI_COPY_BASELINE.md` — consistent initial product wording.
- `docs/RECOVERY_UX_SPEC.md` — recovery-specific behavioral summary.

The baseline product model is a quiet background utility with a small Recovery inbox and compact Settings page. There is deliberately no typing-history screen.

## Design package for future Cursor work

The repository now contains a pre-implementation product/architecture package. The current user-approved workflow is **no Cursor yet**; see `docs/CURRENT_PROJECT_MODE.md`. When Cursor is later introduced, it should execute narrow work packages instead of inventing core behavior. Start future handoff with `docs/CURSOR_HANDOFF_INDEX.md`.

High-value specifications include:

- `docs/PRODUCT_REQUIREMENTS_SPEC.md` — consolidated PRD.
- `docs/MVP_SCOPE_LOCK.md` — explicit in/out scope.
- `docs/DATA_FLOW_AND_PLAINTEXT_BOUNDARIES.md` — where user text may and may not exist.
- `docs/DRAFT_LIFECYCLE_STATE_MACHINE.md` — draft lifecycle semantics.
- `docs/SECURITY_CLASSIFICATION_SPEC.md` — fail-closed secure/private policy.
- `docs/SUBMISSION_AND_CLEAR_DETECTION.md` — completion/clear semantics.
- `docs/RECOVERY_MATCHING_SPEC.md` and `docs/RESTORE_SAFETY_SPEC.md` — safe recovery/write rules.
- `docs/APP_PROFILE_SPEC.md` and `docs/BROWSER_PRIVACY_SPEC.md` — per-app/browser architecture.
- `docs/FUTURE_CONTRACT_CATALOG.md` — conceptual interfaces and data boundaries.
- `docs/IMPLEMENTATION_WORK_PACKAGES.md` — small future Cursor tasks (`WP-*`).
- `docs/PHASE_ACCEPTANCE_GATES.md` — binary stop/go gates.
- `docs/TEST_STRATEGY_MASTER.md` and `docs/MANUAL_TEST_PLAYBOOK.md` — test strategy.

No future agent should begin by asking itself how DraftRescue ought to work; it should implement the already-approved contract for the current work package and surface genuinely open decisions.

## Canonical pre-implementation decisions added

The handoff package now also fixes several implementation-sensitive decisions that future agents should not improvise:

- `docs/DOMAIN_MODEL_CANONICAL.md` — exact aggregate/value-object boundaries and transient plaintext types.
- `docs/IDENTITY_AND_FINGERPRINTING_SPEC.md` — keyed HMAC correlation metadata and installation secret policy.
- `docs/PERSISTENCE_STORAGE_SPEC.md` — SQLite current-state storage, no WAL baseline, no plaintext repository.
- `docs/CRYPTOGRAPHIC_ENVELOPE_SPEC.md` — DPAPI CurrentUser protected-payload format v1.
- `docs/APP_PROFILE_MANIFEST_SCHEMA.md` + `specs/app-profile.schema.json` — machine-validated app profile format.
- `docs/WINDOWS_UIA_CAPABILITY_MATRIX.md` and `docs/BROWSER_DETECTION_RESEARCH_MATRIX.md` — experiment boundaries instead of guessed compatibility.
- `docs/CONCURRENCY_AND_RACE_SPEC.md` — context-generation/stale-result rules.
- `docs/PRIVACY_INVARIANTS_CATALOG.md` — stable invariant IDs for tests and code review.
- `docs/FAILURE_RECOVERY_MATRIX.md` — fail-closed behavior for provider, storage, crypto, restore, and expiry failures.
- `docs/SEQUENCE_DIAGRAMS.md` — normative ordering for capture, preview, restore, and expiry flows.

These documents deepen the future handoff without implementing Phase 1 runtime behavior.

## Testable handoff artifacts

Future implementation work now has stable privacy invariant IDs (`P-001`..`P-030`), a concrete `TEST_CASE_CATALOG.md`, synthetic `SECURITY_TEST_FIXTURES_SPEC.md`, an app support certification template, and a release privacy checklist. Machine-readable schemas/invariants live under `specs/`.


## Deep pre-implementation package

Before Cursor is introduced, this repository intentionally captures the product decisions that an implementation agent must not invent. In addition to the Phase-0 solution skeleton, `docs/` now specifies draft lifecycle, classification-before-read, field identity/rebinding, checkpoint scheduling, persistence/crypto boundaries, shutdown behavior, recovery matching, verified Restore outcomes, app-profile governance, packaging/startup/update safety, threat-to-test traceability, and target support certification.

The current mode remains specification-first. Runtime APIs belonging to later phases are intentionally absent from `src/` until their work package is activated.


## Implementation-ready contract layer

The foundation now also contains exact use-case request/result contracts, stable content-free error codes, transactional repository semantics with persisted `SnapshotSequence`, deterministic profile resolution, deterministic recovery evidence predicates, target-adapter capability contracts, UI component state rules, explicit content-reveal boundaries, and end-to-end recovery scenarios.

The Recovery list is deliberately metadata-only in MVP; opening `Preview` is the normal explicit body-reveal action. Future implementation should start from `docs/USE_CASE_CONTRACTS_V1.md` rather than inventing handlers directly from UI needs.

Key implementation-ready references also include `docs/CONTRACT_PROJECT_OWNERSHIP_MATRIX.md`, `docs/FUTURE_CODE_LAYOUT_PLAN.md`, `docs/APPLICATION_ORCHESTRATION_STATE_MACHINE.md`, `docs/IMPLEMENTATION_READINESS_CHECKLIST.md`, and `docs/USER_VISIBLE_FAILURE_COPY_MATRIX.md`.


## Phase 1 pre-implementation package

Phase 1 is now specified as a strict metadata-only research/implementation boundary. Future work must begin with `docs/PHASE1_PREIMPLEMENTATION_PACKAGE_INDEX.md`.

The package defines:

- content-free WinEvent/UIA event routing;
- dedicated UIA MTA worker behavior;
- audited UIA property safety tiers and machine-readable allowlist;
- bounded cache/property budgets;
- normalized `TargetFieldCandidateMetadata`;
- event deduplication/backpressure/latest-context reconciliation;
- fail-closed provider fault handling;
- Notepad reconnaissance without support promotion;
- zero-target-content-read Phase 1 acceptance criteria;
- content-free machine-readable experiment records;
- privacy-negative and fault-injection protocols.

Chrome/Edge may be inspected for metadata/topology research, but generic browser form capture remains forbidden until the later browser certification phase proves private-mode and sensitive-purpose exclusion before content read.
## Current design depth: Phase 2 pre-implementation

Phase 2 SecureInputGuard is specified through `docs/PHASE2_PREIMPLEMENTATION_PACKAGE_INDEX.md`: typed metadata-only security signals, deterministic fail-closed policy, profile/version-bound positive allow predicates, one-read `AllowedFieldHandle`, revocation/consumption semantics, architecture non-interference tests, native synthetic secure fixtures, and a formal Phase 2 exit gate. No Phase 2 production target-content reader is permitted.

## Current design depth: Phase 3 pre-implementation

Phase 3 is now specified through `docs/PHASE3_PREIMPLEMENTATION_PACKAGE_INDEX.md`. It is the first phase allowed to consume target text, but only through an atomically claimed one-read Phase-2 capability. The package fixes bounded TextPattern reads, certified ValuePattern use, exact-content preservation, explicit oversize failure, transient plaintext ownership, race-safe snapshot envelopes, one-current-state in-memory tracking, empty stabilization, non-leakage architecture tests, and a binary Phase-3 exit gate. Durable persistence remains Phase 4.


## Current design depth: Phase 4 pre-implementation

Phase 4 encrypted persistence is specified through `docs/PHASE4_PREIMPLEMENTATION_PACKAGE_INDEX.md`. The canonical path is protector-before-repository: a Phase-3 current snapshot is encoded into `DraftPayloadV1`, protected with Windows DPAPI `CurrentUser`, converted into `ProtectedDraftRecordV1`, and only then committed to SQLite.

The Phase-4 package fixes SQLite schema v1, `journal_mode=DELETE`, `secure_delete=ON`, `synchronous=EXTRA` durability-first baseline, one serialized writer, persisted monotonic `SnapshotSequence`, metadata-only startup/list queries, DPAPI-protected installation HMAC secret, retention/expiry execution, corruption/quarantine, fail-closed migration, and plaintext-at-rest canary certification.

`secure_delete` is defense-in-depth only; the product does not promise forensic physical erasure. Phase 4 stops before Preview/Copy/Restore.
