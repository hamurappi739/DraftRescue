# DraftRescue

**Never lose typed text again.**

DraftRescue is a privacy-first Windows desktop application intended to recover temporary, unsaved drafts from ordinary text fields after an unexpected tab, window, application, or PC restart.

The repository contains the metadata-safe observation foundation, guarded capture contracts, and the Phase 4 protected local persistence runtime. The desktop shell starts a per-user SQLite store, performs metadata-only retention cleanup, and reports storage availability without exposing draft content. Preview, Copy, Restore, browser integration, cloud sync, and typing history remain intentionally out of scope.

## Non-negotiable privacy boundary

DraftRescue must not become a keylogger.

- No full typing history.
- No passwords, PINs, banking fields, security codes, credentials, or secure input.
- Private/incognito browser contexts are excluded by default.
- No cloud text processing or cloud AI.
- No draft contents in logs.
- Recoverable drafts are temporary, local, encrypted with Windows DPAPI `CurrentUser`, and retention-limited.
- Uncertain secure/private context fails closed: do not persist.

## Solution layout

- `src/DraftRescue.Domain` — core privacy-safe value objects and lifecycle concepts.
- `src/DraftRescue.Application` — use-case contracts and policy abstractions.
- `src/DraftRescue.Platform.Windows` — Windows observation, DPAPI, and per-user storage adapters.
- `src/DraftRescue.Infrastructure` — protected SQLite repository, schema, retention, and corruption handling.
- `src/DraftRescue.Desktop` — Avalonia composition root, persistence startup, and UI shell.
- `tests/DraftRescue.Tests` — unit, architecture, privacy, persistence, and desktop lifecycle tests.
- `docs` — architecture, threat model, ADRs, execution rules, and canonical project context.

## Build and test

On a Windows x64 machine with a compatible stable .NET 8 SDK installed (see `global.json` and `docs/SDK_REPRODUCIBILITY_POLICY.md`):

```powershell
./scripts/verify.ps1
```

or:

```cmd
scripts\verify.cmd
```

The verification scripts fail on the first failing scope, SDK, restore, build, or test step. The current solution builds on Windows x64 with .NET 8 and the complete test suite is green.

To launch the desktop shell locally:

```powershell
dotnet run --project .\src\DraftRescue.Desktop\DraftRescue.Desktop.csproj
```

The runtime uses the current Windows user's `%LOCALAPPDATA%\DraftRescueData` directory. If DPAPI or the local store is unavailable, the application remains fail-closed and shows a non-sensitive warning instead of writing plaintext.

## Current status

Phase 4 implementation is complete at the code and synthetic-certification level. The latest run builds with 0 warnings and 0 errors and passes **181/181 tests**. SQLite schema validation, DPAPI boundary handling with content-free failure reasons, dual payload/installation-secret runtime probing, installation-secret startup validation, explicit runtime checkpoint-worker composition, bounded checkpoint scheduling and execution, one-retry budgeting, monotonic protected writes, retention cleanup, corruption classification, process-kill rollback, and plaintext-at-rest canary guards are covered. The final Phase 4 gate is currently **Inconclusive only because this host cannot provide the two external certifications**: a positive DPAPI `CurrentUser` roundtrip and a real controlled disk-full fixture. No plaintext fallback is used to hide those conditions.

The desktop UI follows the quiet Apple-inspired direction: a compact recovery surface, explicit privacy copy, language switching, light/dark themes, and a localized storage-health banner. It never renders draft bodies during startup or list loading.

## UI/UX design baseline

The UI implementation follows:

- `docs/UI_UX_MASTER_SPEC.md` — complete minimal UI direction and wireframes.
- `docs/UI_STATE_MODEL.md` — presentation states and security boundaries for ViewModels.
- `docs/UI_COPY_BASELINE.md` — consistent initial product wording.
- `docs/RECOVERY_UX_SPEC.md` — recovery-specific behavioral summary.

The baseline product model is a quiet background utility with a small Recovery inbox and compact Settings page. There is deliberately no typing-history screen.

## Design package for future Cursor work

The repository also contains the canonical product/architecture package used to constrain future work. New implementation should execute one documented work package at a time and start with `docs/CURSOR_HANDOFF_INDEX.md`.

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

These documents preserve the accepted privacy and architecture decisions that the runtime must continue to respect.

## Testable handoff artifacts

Future implementation work now has stable privacy invariant IDs (`P-001`..`P-030`), a concrete `TEST_CASE_CATALOG.md`, synthetic `SECURITY_TEST_FIXTURES_SPEC.md`, an app support certification template, and a release privacy checklist. Machine-readable schemas/invariants live under `specs/`.


## Deep pre-implementation package

Before Cursor is introduced, this repository intentionally captures the product decisions that an implementation agent must not invent. In addition to the Phase-0 solution skeleton, `docs/` now specifies draft lifecycle, classification-before-read, field identity/rebinding, checkpoint scheduling, persistence/crypto boundaries, shutdown behavior, recovery matching, verified Restore outcomes, app-profile governance, packaging/startup/update safety, threat-to-test traceability, and target support certification.

Runtime APIs are activated only when their documented work package begins; unsupported workflows remain absent by design.


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


## Phase 4 protected persistence

Phase 4 encrypted persistence follows `docs/PHASE4_PREIMPLEMENTATION_PACKAGE_INDEX.md`. The canonical path is protector-before-repository: a Phase-3 current snapshot is encoded into `DraftPayloadV1`, protected with Windows DPAPI `CurrentUser`, converted into `ProtectedDraftRecordV1`, and only then committed to SQLite.

The Phase-4 package fixes SQLite schema v1, `journal_mode=DELETE`, `secure_delete=ON`, `synchronous=EXTRA` durability-first baseline, one serialized writer, persisted monotonic `SnapshotSequence`, metadata-only startup/list queries, DPAPI-protected installation HMAC secret, retention/expiry execution, corruption/quarantine, fail-closed migration, and plaintext-at-rest canary certification.

`secure_delete` is defense-in-depth only; the product does not promise forensic physical erasure. Phase 4 stops before Preview/Copy/Restore, and current exit blockers are recorded in `artifacts/phase4-exit-gate/PHASE4-EXIT-GATE.json` after running the gate scripts.
