# DraftRescue — Codex Handoff: START HERE

**Handoff date:** 2026-09-02  
**Target:** Windows x64, C#, .NET 8, Avalonia UI, MVVM  
**Product:** DraftRescue — privacy-first recovery layer for unsaved text  
**Status:** specification-first foundation complete through Phase 4; Phase 1, Phase 2 and Phase 3 gates are closed, and Phase 4 WP4.1–WP4.7 gates are passed. WP4.8 final exit review is complete but Inconclusive because target-environment DPAPI and real disk-full evidence are still required; process-kill rollback now passes.

---

## 0. Your role

You are taking over DraftRescue from a specification-first design phase. You are **not** being asked to redesign the product.

Your job is to:

1. preserve the accepted product/privacy/architecture decisions;
2. verify the existing Phase-0 skeleton on a real Windows + .NET 8 environment;
3. implement one narrow work package at a time;
4. run the named tests and stop at the current phase boundary;
5. surface genuinely open decisions instead of inventing broad behavior;
6. keep all user text local, transient where possible, encrypted before persistence, and absent from logs/telemetry/network.

Do not begin by asking "how should DraftRescue work?". That is already specified.

---

## 1. Canonical source hierarchy

Read these first, in this order:

1. `docs/DRAFTRESCUE_MASTER_CONTEXT.md` — original canonical product context.
2. `AGENTS.md` — repository-wide agent rules.
3. `docs/CURRENT_PROJECT_MODE.md` — explains that the project was intentionally developed specification-first before coding agents.
4. `docs/CODEX_EXECUTION_GUIDE.md` — this handoff's execution rules.
5. `docs/CURSOR_HANDOFF_INDEX.md` — historical mandatory reading map; treat it as a general coding-agent handoff index even though you are Codex.
6. `docs/ADR_DECISION_SUMMARY_V1.md` and `docs/adr/` — 63 accepted ADRs.
7. `docs/OPEN_DECISIONS.md` — only the items here remain intentionally unresolved.
8. `docs/IMPLEMENTATION_WORK_PACKAGES.md` — the phase/work-package sequence.
9. `docs/PHASE_ACCEPTANCE_GATES.md` — binary stop/go gates.
10. `docs/TEST_CASE_CATALOG.md` and `specs/test-catalog.v1.json` — canonical test registry.

If a later implementation convenience conflicts with product/privacy invariants, **the product/privacy invariant wins** unless the user explicitly changes it.

---

## 2. What is already in this ZIP

This handoff contains the complete working package produced so far:

- `.sln`, `.csproj`, root build/package props;
- current Phase-0 source skeleton;
- current tests;
- all Markdown architecture/product/security/UI documents;
- all accepted ADRs;
- all JSON schemas, policies, fixtures, traceability artifacts;
- PowerShell/CMD verification scripts;
- file inventory, manifest and checksums;
- Phase 1, 2, 3 and 4 pre-implementation packages;
- broader Phase 5–10 product/architecture specifications;
- this Codex-specific takeover package.

The package is specification-heavy by design. Do not discard or regenerate the repository from scratch.

A Codex foundation audit is included as `CODEX_FOUNDATION_AUDIT_2026-09-02.md`, with its disposition in `CODEX_AUDIT_RESOLUTION_2026-09-02.md`. Audit findings F-001, F-002, F-003 and the SDK-range portion of F-004 are resolved in this package.

This audited/repaired handoff contains **1420 regular files**, including **294 Markdown files**. `FOUNDATION_SHA256SUMS.txt` contains 1420 entries because the checksum file intentionally excludes itself.

For the original project-generating AI, the current large continuation prompt is `ORIGINAL_AI_MAXIMUM_WORK_PROMPT_2026-09-10.md`. It is intentionally bounded to WP4.8 blocker resolution and must not be used as permission to skip privacy gates or start Phase 5.

For the remaining target-host certification, use `docs/PHASE4_TARGET_CERTIFICATION_RUNBOOK.md` and `scripts/run_phase4_target_certification.ps1`. They collect typed evidence only and never manufacture a real disk-full Pass.

---

## 3. Current implementation reality

This distinction is critical.

### Implemented today

The repository contains the Phase-0 skeleton plus the completed Phase-1
observation plane and Phase-2 metadata-only security boundary:

- `DraftRescue.Domain`
- `DraftRescue.Application`
- `DraftRescue.Platform.Windows`
- `DraftRescue.Infrastructure`
- `DraftRescue.Desktop`
- `DraftRescue.Tests`
- minimal Avalonia shell;
- basic privacy-safe value objects/contracts;
- basic fail-closed `CaptureEligibility` tests;
- architecture/privacy skeleton tests.
- WP-1.1 content-free foreground WinEvent source with bounded callback queue;
- foreground identity normalization and deterministic disposal tests;
- WP-1.2 focused UIA metadata source on a dedicated MTA worker, with no target-content reads;
- bounded UIA metadata and idempotent-disposal tests.
- WP-1.3 metadata-only coordinator with generation, stale-sequence and own-process handling;
- coordinator tests for deduplication, transient/unsupported states, source faults and disposal.
- WP-1.4 synthetic burst/fault/lifecycle harness plus Notepad and managed WPF metadata-only reconnaissance with content-safe JSON evidence; corrected long-soak, integrity, provider-stress, package-integrity and interactive WPF confirmation evidence are recorded. Phase-1 gate passes via the validated interactive confirmation; automated repeat remains a non-interactive environment diagnostic.
- WP-2.1..WP-2.2 typed security signals and deterministic fail-closed policy evaluator;
- WP-2.3..WP-2.5 protected/unknown/private/sensitive-purpose denials and a profile-bound synthetic positive predicate;
- WP-2.6 opaque one-read `AllowedFieldHandle` issuer/validator with monotonic expiry, generation/binding/profile checks, atomic claim and revocation;
- WP-2.7 native Windows Forms/UIA structural fixture plus executable 24-case fault matrix and stress evidence; Phase-2 exit review passes without target-content reads.
- WP-3.1 one-shot reader contract and typed outcomes; WP-3.2 bounded certified TextPattern adapter; WP-3.3 certified bounded ValuePattern adapter; WP-3.4..WP-3.6 exact transient snapshots, generation/sequence-safe current-state tracking and two-observation empty stabilization. Phase-3 application gate passes without durable persistence or UI body exposure.
- WP-4.1 protected-record model, ciphertext-only repository contract and canonical SQLite schema declaration; contract/schema gate passes (`120/120`) while runtime persistence remains disabled.
- WP-4.2 strict `DRP1` envelope, DPAPI `CurrentUser` protector and 256-bit installation-secret lifecycle; DPAPI boundary gate passes (`127/127`) with fail-closed handling when the test host profile is unavailable.
- WP-4.3 SQLite schema/bootstrap and serialized protected repository; transactional stale-sequence, metadata-only listing and expiry/delete semantics pass (`133/133`).
- WP-4.4 protect-before-repository checkpoint coordinator and metadata-only retention execution; generation/cancellation/failure outcomes pass (`139/139`).
- WP-4.6 typed corruption classification, local quarantine, and non-destructive migration policy pass (`142/142`); no salvage, auto-drop, or network recovery path exists.
- WP-4.7 synthetic plaintext-at-rest canary scan and bounded crash/lock/disk-full fault certification pass (`148/148`); no canary matches or plaintext fallback.
- WP-4.8 final evidence combiner is Inconclusive: DPAPI CurrentUser positive roundtrip and real disk-full certification remain blockers; the OS-level process-kill rollback probe and deterministic BeforeCommit disk-full rollback pass, with no unsafe fallback added. Gate diagnostics are schema v2 with typed required/pending items and a passing artifact privacy guard; the latest full run is build `0/0`, tests `181/181`, `phase4Exit=false`, and the DPAPI probe records structural `failureStage=Protect`/`failureReason=Cryptographic` on this host.

### NOT implemented today

Despite very detailed specifications, the following are **not production-implemented** yet:

- automated WPF focused-metadata repeatability is still `0/5` in the non-interactive runner, but final Phase-1 exit acceptance is now passed via the bounded interactive confirmation (`8/8`);
- Phase 4 corruption/migration/certification and recovery UI behavior/later product integration (WP4.1–WP4.4 gates passed; no reader is registered in the product composition root yet);
- target text reading;
- in-memory draft tracking;
- SQLite storage;
- DPAPI protector;
- installation HMAC key storage;
- retention service runtime;
- Recovery UI behavior beyond shell/mock baseline;
- Preview decryption;
- Copy;
- Restore;
- browser capture;
- Discord/Electron adapters;
- production app-profile certification;
- packaging/startup/update integration.

The repository is intentionally documentation-heavy because these behaviors were designed before implementation.

---

## 4. Current phase status

| Phase | Design status | Implementation status | Gate |
|---|---|---|---|
| 0 Architecture | Complete foundation | Verified on Windows/.NET 8 | Pass |
| 1 Active App + Field Detection | Deep pre-implementation package complete | WP-1.1..WP-1.4 implemented; interactive gate 8/8 | Pass |
| 2 Secure Field Guard | Deep pre-implementation package complete | WP-2.1..WP-2.8 implemented within zero-content-read boundary | Pass |
| 3 Draft Tracking Prototype | Deep pre-implementation package complete | WP-3.1, WP-3.4..WP-3.6 application semantics implemented; UIA adapters pending | Must satisfy `PHASE3_EXIT_CRITERIA.md` |
| 4 Encrypted Local Persistence | Deep pre-implementation package complete | WP4.1–WP4.8 reviewed; exit Inconclusive pending target-environment evidence | Must satisfy `PHASE4_EXIT_CRITERIA.md` |
| 5 Recovery UI | Product/UI specs exist | Not implemented | Implement only after Phase 4 GO |
| 6 Restore | Safety/matching specs exist | Not implemented | Implement only after Phase 5 GO |
| 7 Browsers | Research/privacy specs exist | Not implemented | Chrome then Edge, independently certified |
| 8 Electron | Roadmap/spec baseline exists | Not implemented | Discord first |
| 9 App Profiles | Profile architecture specified | Not implemented/hardened | Certification/versioning required |
| 10 Hardening | Test/threat/release specs exist | Not implemented | Final privacy/resource/release gate |

**Do not infer that "documented" means "implemented".**

---

## 5. First action in Codex

### Do this before Phase 1

On a real Windows x64 machine with a stable .NET 8 SDK compatible with `global.json`:

```powershell
./scripts/verify.ps1
```

or:

```cmd
scripts\verify.cmd
```

Then launch the Avalonia desktop shell manually.

### Required outcome

- NuGet restore succeeds;
- the entire solution builds;
- all existing tests pass;
- the Avalonia shell launches;
- architecture project references remain correct;
- no Phase 1+ functionality is accidentally introduced while fixing Phase 0.

Phase 0 is now green. Continue only with the explicitly approved Phase 1 work package and stop at its gate.

The repository-generation environment could not complete this Windows SDK gate, so **no one has honestly completed it yet**.

The corrected `scripts/verify.ps1` must now fail on missing SDK or any non-zero restore/build/test exit code. Record the exact SDK version used when this gate first passes. See `docs/SDK_REPRODUCIBILITY_POLICY.md`.

---

## 6. Non-negotiable privacy/security rules

Treat these as product requirements, not suggestions.

### DraftRescue must never become a keylogger

- no global keystroke-history stream;
- no full typing history;
- no revision history of drafts;
- no passwords, PINs, OTP/security codes, credentials, banking/payment fields;
- no secure/private content reads when the system cannot prove the field is eligible;
- private/incognito browser contexts deny by default;
- no cloud text processing;
- no cloud AI;
- no network transmission of draft text;
- no draft text in logs, exception messages, metrics labels, traces or support bundles;
- no raw URL/window-title/field-label persistence for decorative UI;
- uncertain security state fails closed.

### Classification before content read

The intended pipeline is:

```text
content-free context observation
        -> metadata-only candidate
        -> SecureInputGuard
        -> one-read AllowedFieldHandle
        -> one bounded text read
        -> one current in-memory snapshot
        -> protect/encrypt
        -> protected record repository
```

There must not be an alternate generic path that reads text first and filters later.

### Restore is a different authority

A capture capability **cannot** authorize Restore.

Restore requires a fresh target lookup, fresh security classification, strong deterministic match evidence, target revalidation and adapter-specific write behavior.

---

## 7. Stable invariant catalogs

The current package contains:

- **50 privacy invariants** (`P-001` … `P-050`);
- **50 correctness invariants** (`C-001` … `C-050`);
- **63 accepted ADRs** (`0001` … `0063`);
- **229 registered tests** in `specs/test-catalog.v1.json`.

Before changing code for any work package:

1. identify applicable `P-*` invariants;
2. identify applicable `C-*` invariants;
3. identify named test IDs;
4. state what is explicitly out of scope.

Do not renumber existing IDs.

---

## 8. Test registry summary

Canonical machine-readable count: **229 tests**.

Current categories:

- ARC 8
- BRW 5
- CAP 8
- CERT 2
- E2E 12
- ERR 1
- EXP 2
- FI 4
- LIFE 14
- LOG 4
- MAT 3
- NET 1
- OBS 10
- P2F 6
- P3F 4
- P4F 20
- PERF 7
- PKG 1
- PRF 14
- PST 20
- PTX 15
- READ 11
- RMF 5
- RST 9
- SEC 23
- SNAP 7
- TRK 6
- UI 7

Do not create duplicate IDs. Add new tests only through the canonical catalog and keep Markdown/JSON traceability synchronized.

---

## 9. Phase-by-phase implementation plan

### Phase 0 — verified; do not expand

Read:

- `README.md`
- `ARCHITECTURE.md`
- `DEVELOPMENT_RULES.md`
- `PHASE_ACCEPTANCE_GATES.md`

Result: restore/build/tests and Avalonia shell launch passed on Windows 10 x64 with .NET SDK 8.0.424. Phase 0 is green.

---

### Phase 1 — Active App + Field Detection

Mandatory entry:

- `docs/PHASE1_PREIMPLEMENTATION_PACKAGE_INDEX.md`
- every document/spec referenced from it.

Core boundary:

> **Phase 1 target-content read count must remain zero.**

Implement in narrow WPs:

- foreground WinEvent lifecycle;
- UIA MTA focus worker;
- audited metadata acquisition;
- candidate normalization;
- event coalescing/reconciliation;
- provider failure/backoff;
- synthetic + Notepad reconnaissance;
- performance/soak gate.

Do not implement a content reader in Phase 1.

Minimum named evidence includes:

- `OBS-001..010`
- `EXP-001..002`
- `FI-001..004`
- `PERF-001..007`
- `CERT-001..002`

Open experiment outputs still required:

- final WinEvent/UIA combination;
- numeric timeout/coalescing budgets;
- target topology observations on real Windows builds;
- minimum supported Windows version evidence.

Stop at `PHASE1_EXIT_CRITERIA.md`.

---

### Phase 2 — SecureInputGuard

Mandatory entry:

- `docs/PHASE2_PREIMPLEMENTATION_PACKAGE_INDEX.md`
- `SECURITY_SIGNAL_MODEL_V1.md`
- `SECURITY_POLICY_EVALUATION_SPEC.md`
- `ALLOWED_FIELD_HANDLE_CAPABILITY_SPEC.md`
- `PHASE2_NONINTERFERENCE_ARGUMENT.md`
- `PHASE2_EXIT_CRITERIA.md`

Important model:

- security signal states are typed (`Known`, `Unknown`, `Unavailable`, `Failed`);
- unknown/unavailable/failed required security signals deny;
- `IsPassword=true` hard-denies;
- `IsPassword=false` does **not** imply safe;
- an ordinary editable field is not allowed unless a version-bound positive profile predicate allows it;
- `AllowedFieldHandle` is one-read, non-serializable, generation/binding/profile-bound;
- claim consumes it;
- a capture handle cannot authorize Restore;
- Phase-2 DI graph must contain no target-content reader.

Run all SEC/CAP/P2F/architecture tests referenced by the Phase-2 matrix.

Stop at `PHASE2_EXIT_CRITERIA.md`.

---

### Phase 3 — Authorized Content Read + In-Memory Draft Tracking

Mandatory entry:

- `docs/PHASE3_PREIMPLEMENTATION_PACKAGE_INDEX.md`
- `CONTENT_READ_PIPELINE_SPEC.md`
- `ONE_SHOT_TEXT_READER_CONTRACT.md`
- `FIELD_TEXT_SNAPSHOT_MODEL.md`
- `TEXT_READ_STRATEGY_MATRIX.md`
- `PLAINTEXT_MEMORY_LIFETIME_SPEC.md`
- `IN_MEMORY_DRAFT_TRACKER_SPEC.md`
- `PHASE3_EXIT_CRITERIA.md`

Rules:

- every content read atomically consumes a fresh Phase-2 capability;
- `TextPattern` uses finite `GetText(configuredLimit + 1)`; never `GetText(-1)`;
- `ValuePattern` only for explicitly certified bounded surfaces;
- no generic LegacyIAccessible/keyboard/clipboard fallback;
- oversize is typed `TooLarge`, never silently truncated;
- text is preserved exactly: no Trim, newline normalization, Unicode normalization or semantic cleanup;
- managed string memory is copy-minimized; do not make false zeroization claims;
- tracker holds one current state only, no revisions;
- one empty read does not clear a draft;
- stale/out-of-order results never overwrite newer state;
- no durable persistence yet.

Run READ/SNAP/P3F plus applicable CAP/SEC/ARC regression tests.

Stop at `PHASE3_EXIT_CRITERIA.md`.

---

### Phase 4 — Encrypted Local Persistence

Current progress: WP4.1–WP4.8 are reviewed. The exit combiner now records schema-v2 typed evidence and distinguishes expected DPAPI unavailability from harness failure; the latest result remains Inconclusive with exactly two target-environment pending items. The next bounded action is to resolve those blockers and rerun the exit gate. Do not enable Preview, Copy or Restore.

Mandatory entry:

- `docs/PHASE4_PREIMPLEMENTATION_PACKAGE_INDEX.md`
- `PHASE4_IMPLEMENTATION_BOUNDARY.md`
- `PROTECTED_RECORD_MODEL_V1.md`
- `DPAPI_PAYLOAD_FORMAT_V1.md`
- `SQLITE_SCHEMA_V1_SPEC.md`
- `SQLITE_CONNECTION_PRAGMAS_SPEC.md`
- `INSTALLATION_SECRET_LIFECYCLE_SPEC.md`
- `PERSISTENCE_COORDINATOR_SPEC.md`
- `CHECKPOINT_COMMIT_STATE_MACHINE.md`
- `PLAINTEXT_AT_REST_CERTIFICATION.md`
- `PHASE4_FAULT_INJECTION_MATRIX.md`
- `PHASE4_EXIT_CRITERIA.md`

Canonical write path:

```text
FieldTextSnapshot
  -> DraftPayloadV1
  -> DPAPI CurrentUser
  -> ProtectedDraftRecordV1
  -> SQLite
```

Rules:

- repository cannot accept plaintext types;
- DPAPI `CurrentUser`; no `LocalMachine`; no plaintext fallback;
- separate random 256-bit installation HMAC secret, itself DPAPI-protected;
- SQLite v1 stores one current row per `DraftId`;
- persist `SnapshotSequence` and reject stale writes inside transaction;
- baseline `journal_mode=DELETE`, `secure_delete=ON`, `synchronous=EXTRA`, bounded busy timeout;
- no WAL without a new approved ADR;
- one logical writer;
- no revision table/history;
- list/expiry/discard-all must not decrypt bodies;
- corruption does not trigger raw-page/plaintext salvage;
- unknown schema does not auto-drop/recreate;
- secure-delete is defense-in-depth, not an SSD forensic-wipe promise.

Critical certification:

- synthetic plaintext canaries must be absent from DraftRescue-owned DB/journal/temp/settings/log/quarantine artifacts;
- DPAPI failure => zero repository writes;
- crash/kill tests leave valid old-or-new transaction state;
- stale writes are rejected;
- disk full/locked store causes bounded failure and no plaintext temp fallback.

Run PST/P4F and applicable ARC/PTX/LOG regression tests.

Stop at `PHASE4_EXIT_CRITERIA.md`. **Do not implement Preview in Phase 4.**

---

### Phase 5 — Recovery UI

The broad UI design is already specified, but this phase has not yet received the same dedicated pre-implementation package depth as Phases 1–4.

Before coding, read:

- `UI_UX_MASTER_SPEC.md`
- `UI_STATE_MODEL.md`
- `UI_COMPONENT_STATE_SPEC.md`
- `UI_DESIGN_TOKENS.md`
- `UI_COPY_BASELINE.md`
- `SAFE_PRESENTATION_METADATA_SPEC.md`
- `CONTENT_REVEAL_AND_MEMORY_LIFETIME_SPEC.md`
- `RECOVERY_UX_SPEC.md`
- `CLIPBOARD_POLICY_SPEC.md`
- `USE_CASE_CONTRACTS_V1.md`

Canonical UI behavior:

- quiet background utility;
- no history screen;
- Recovery list is metadata-only;
- cards show coarse application/surface/time metadata, not raw URL/title/field labels;
- no automatic draft snippets;
- Preview is explicit body reveal;
- Settings are minimal;
- Close window != Quit; tray/background lifecycle;
- Copy is explicit; no automatic clipboard-clear timer.

Implement only after Phase 4 is green.

Run UI-* plus applicable PST/PTX/LIFE tests and Phase-5 acceptance gate.

---

### Phase 6 — Restore

Read:

- `RECOVERY_MATCHING_SPEC.md`
- `RECOVERY_EVIDENCE_LATTICE.md`
- `RESTORE_SAFETY_SPEC.md`
- `RESTORE_WRITE_VERIFICATION_SPEC.md`
- `FIELD_IDENTITY_AND_REBINDING_SPEC.md`
- `TARGET_ADAPTER_CONTRACTS.md`
- `COMMAND_IDEMPOTENCY_AND_SINGLE_FLIGHT_SPEC.md`

Rules:

- explicit user action only;
- fresh target lookup;
- fresh security recheck;
- deterministic evidence lattice; no universal numeric confidence threshold;
- ambiguity => no direct Restore;
- no `force=true` bypass;
- target revalidation immediately before write;
- failed or applied-but-unverified restore keeps recovery copy;
- only verified success can remove payload;
- restore single-flight per `DraftId`;
- no generic SendKeys/paste fallback unless a future target-specific ADR explicitly allows it.

Run MAT/RST/RMF plus privacy/race regression tests.

---

### Phase 7 — Browser Support

Order:

1. Chrome research with synthetic pages;
2. prove Incognito deny before ordinary capture is enabled;
3. certify one normal editor surface;
4. test login/payment/browser-chrome negatives;
5. test recovery/cross-origin restrictions;
6. repeat independently for Edge.

Read:

- `BROWSER_PRIVACY_SPEC.md`
- `BROWSER_PRIVATE_MODE_EXPERIMENT_PROTOCOL.md`
- `BROWSER_DETECTION_RESEARCH_MATRIX.md`
- `BROWSER_ORIGIN_FINGERPRINT_SPEC.md`
- `PROFILE_VERSION_COMPATIBILITY_POLICY.md`

Unknown browser version/tree => fail closed.

Run BRW/SEC/PRF/CERT tests.

---

### Phase 8 — Electron

Start with Discord only.

Required before support:

- accessibility/profile research;
- login/credential negative cases;
- composer tracking/completion semantics;
- recovery/matching/restore tests;
- update/version fallback safety.

Telegram Desktop is later and must not be bundled automatically.

---

### Phase 9 — App Profiles

Harden app-specific behavior behind profile contracts.

Rules:

- global security denies cannot be weakened by a profile;
- profile resolution is deterministic;
- ambiguity fails closed;
- supported target versions are certified ranges;
- unknown/incompatible versions do not inherit support;
- MVP profile policy ships with signed app releases; no remote policy channel.

Run PRF/CERT and related SEC tests.

---

### Phase 10 — Hardening & Privacy

Final gate must include:

- full privacy negative matrix;
- crash/restart/race/retention tests;
- log/storage plaintext canary audit;
- 30-minute+ resource soak/leak tests;
- provider fault injection;
- packaging/startup/update security review;
- known unsupported-case documentation;
- release privacy checklist with no critical failure.

Do not claim release-ready until Phase 10 is green.

---

## 10. Intentionally open decisions

Do **not** silently resolve these inside unrelated implementation tasks. They require focused experiments/ADRs:

1. exact minimum supported Windows version;
2. final measured Phase-1 event/API combination and numeric budgets;
3. exact secure-field detector signals per native/Chromium/Electron framework beyond hard password/protected signals;
4. exact private-browsing detection strategy per browser/version;
5. exact snapshot acquisition strategy per control family based on real fixtures;
6. exact debounce/max-dirty-age values;
7. profile-specific strong-match predicates for each supported application;
8. direct Restore mechanism per target/control family;
9. final packaging/update/startup mechanism;
10. release diagnostic-log default and rotation quota.

When blocked on one of these:

- run the narrowest experiment possible;
- capture the result using the repository experiment schema;
- write/update an ADR if a durable decision is made;
- add tests;
- fail closed until evidence exists.

---

## 11. Required working style for every Codex task

### Before editing

State:

- current phase and WP;
- documents read;
- projects/files expected to change;
- applicable `P-*` invariants;
- applicable `C-*` invariants;
- named acceptance tests;
- explicit out-of-scope items;
- whether an open decision is involved.

### During editing

- make the smallest coherent change;
- do not refactor unrelated code;
- do not pull later phases forward;
- do not weaken fail-closed behavior to make tests pass;
- do not add telemetry/cloud/network behavior;
- do not log raw UIA strings or captured content;
- do not invent support for applications that have not been certified.

### After editing

Report:

- files changed;
- exact build command/result;
- exact test command/result;
- named test IDs executed/added;
- invariant coverage;
- experiment results if applicable;
- unresolved issues;
- explicit confirmation that you stopped before the next WP/phase.

---

## 12. What not to do

Do not:

- rebuild the architecture from scratch;
- replace the project structure casually;
- introduce global keyboard capture;
- capture first and classify later;
- treat `IsPassword=false` as a safety proof;
- persist plaintext;
- add a revision/history table;
- add WAL casually;
- send draft text to network/AI/telemetry;
- use raw URL/title/labels as recovery-list decoration;
- decrypt all drafts to render the list;
- choose the first matching app profile when multiple match;
- use numeric "confidence > X" as universal recovery policy;
- offer force-restore for ambiguous targets;
- use clipboard/SendKeys as a generic restore fallback;
- promote Notepad/Chrome/Edge/Discord to Supported because a single happy-path test worked;
- continue to the next phase in the same task after a gate succeeds unless the user explicitly asks.

---

## 13. Definition of success

The product is successful when a user loses a meaningful unsaved text draft and DraftRescue safely returns it with the feeling:

> **“Фух. Оно меня спасло.”**

That outcome is only acceptable if the product simultaneously preserves the privacy boundary: it must not behave like a keylogger, must not collect sensitive input, and must fail closed when uncertain.

---

## 14. Immediate takeover checklist

Use this exact checklist now:

- [ ] Read this file completely.
- [ ] Read `docs/DRAFTRESCUE_MASTER_CONTEXT.md`.
- [ ] Read `AGENTS.md`.
- [ ] Read `docs/ADR_DECISION_SUMMARY_V1.md`.
- [ ] Read `docs/OPEN_DECISIONS.md`.
- [ ] Read `docs/IMPLEMENTATION_WORK_PACKAGES.md`.
- [ ] Read `docs/PHASE_ACCEPTANCE_GATES.md`.
- [ ] Inspect current `src/` and `tests/` without rewriting them.
- [ ] Read `CODEX_FOUNDATION_AUDIT_2026-09-02.md` and `CODEX_AUDIT_RESOLUTION_2026-09-02.md`.
- [x] Run `scripts/verify.ps1` on Windows with a compatible .NET 8 SDK.
- [x] Fix Phase 0 only if necessary.
- [ ] Report Phase-0 verification result to the user.
- [x] Phase 0 is green; begin only the explicitly approved Phase 1 WP.
