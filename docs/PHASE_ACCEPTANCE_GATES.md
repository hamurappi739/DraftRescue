# Phase Acceptance Gates

This document turns the roadmap into binary review gates. Future coding agents must stop at the requested gate.

## Phase 0 — Architecture

Pass when:

- solution restores/builds/tests on Windows .NET 8;
- Avalonia shell launches;
- dependency boundaries match architecture;
- privacy invariants and threat model exist;
- no observation/text capture/persistence/restore implementation exists.

## Phase 1 — Active App + Field Detection

Pass when all of `PHASE1_EXIT_CRITERIA.md` is satisfied, including:

- foreground/focus observation works in controlled synthetic targets and Notepad reconnaissance;
- callbacks emit only content-free bounded envelopes and perform no provider traversal;
- generic UIA queries/cache requests contain only audited Tier A/B properties;
- `TargetFieldCandidateMetadata` normalization is deterministic and bounded;
- duplicate/out-of-order/event-storm routing converges on latest context;
- stale results are rejected by context generation;
- **target text read count is zero across Phase 1**;
- no keyboard hook/global keystroke stream;
- provider failures/timeouts/integrity mismatch are typed and fail closed;
- subscriptions clean up idempotently;
- idle/active resource measurements and a 30-minute focus soak are recorded;
- schema-valid experiment records pass content-safety audit;
- Notepad remains Experimental; Phase 1 alone cannot promote app support.

Named minimum evidence: OBS-001..010, EXP-001/002, FI-001..004, PERF-001..007, CERT-001/002.

## Phase 2 — Secure Field Guard

Pass when:

- password/protected fields hard deny;
- ordinary supported test field explicitly allows;
- uncertain/provider-error cases deny;
- text-reader mock proves it is not invoked for denied cases where architecture allows pre-read gating;
- credential/banking metadata/profile negative fixtures pass;
- no content-based secret classifier introduced.

## Phase 3 — Draft Tracking Prototype

Pass when:

- one supported safe target produces current snapshots only;
- replacement/updating does not create revision history;
- focus switching does not mark completion;
- manual clear removes obsolete current draft according to policy;
- completion/loss state transitions are unit tested;
- no durable plaintext storage yet unless Phase 4 begins explicitly.

## Phase 4 — Encrypted Local Persistence

Pass only when the full `PHASE4_EXIT_CRITERIA.md` gate passes, including:

- protector-before-repository boundary is enforced;
- DPAPI `CurrentUser` works with no plaintext/LocalMachine fallback;
- installation HMAC secret is random and DPAPI-protected;
- schema v1 has one current row per DraftId and persisted `SnapshotSequence`;
- `journal_mode=DELETE`, `secure_delete=ON`, `synchronous=EXTRA` baseline and bounded busy timeout are verified;
- stale/equal-conflict/crash transaction tests pass;
- startup/list/expiry/discard-all paths do not decrypt bodies;
- canary plaintext is absent from DraftRescue-owned DB/journal/temp/log/settings artifacts;
- corrupt/incompatible storage fails closed without salvage/auto-upload/auto-drop;
- retention cleanup passes after restart and expired rows remain hidden even when deletion is delayed;
- no revision/history accumulation;
- no Preview/Copy/Restore implementation is bundled into Phase 4.

## Phase 5 — Recovery UI

Pass when:

- Empty/Ready/Error states implemented;
- Preview/Discard behavior follows specs;
- expired race handled;
- UI contains no storage/decryption/platform logic;
- Copy follows `CLIPBOARD_POLICY_SPEC.md`: explicit action only, no automatic clear.

## Phase 6 — Restore

Pass when:

- fresh security re-check occurs at restore time;
- strong multi-signal match required;
- ambiguous target cannot be forced;
- target change race aborts;
- failed restore keeps recoverable draft;
- successful verified restore removes payload;
- no automatic restore.

## Phase 7 — Browser Support

Pass Chrome first, then Edge independently when:

- normal supported editor works;
- private/incognito always denies in tested matrix;
- browser chrome fields denied;
- login/payment negative surfaces denied;
- cross-origin restore blocked;
- browser update/unknown tree fails closed.

## Phase 8 — Electron Apps

Pass Discord first only after:

- accessibility behavior profiled;
- login/credential surfaces denied;
- compose completion semantics tested;
- restore target matching tested;
- app update fallback safe.

## Phase 9 — App Profiles

Pass when:

- app-specific branches are moved behind profile contracts;
- global secure denies cannot be weakened by a profile;
- capability/version matrix exists per profile.

## Phase 10 — Hardening & Privacy

Pass when:

- complete privacy negative matrix passes;
- crash/restart/retention/race tests pass;
- resource leak/performance tests pass;
- log/storage canary audit passes;
- packaging/update/startup behavior has explicit security review;
- known unsupported cases documented;
- `RELEASE_PRIVACY_CHECKLIST.md` has no critical failure.


## Cross-phase accepted gates

- Any persisted snapshot path must satisfy C-001/C-012: stale sequence is rejected inside repository transaction.
- Any Recovery-list path must satisfy C-010: no body decryption.
- Any profile work must pass deterministic resolution tests; ambiguity/unknown version fails closed.
- Any recovery matcher must use `RECOVERY_EVIDENCE_LATTICE.md`; no global score threshold.
- Any Restore UI/service must enforce single-flight per DraftId and keep the record for applied-but-unverified outcomes.
- Any expected operational failure must map to a stable content-free `DR-*` code.

## Phase 3 gate — authorized content read + in-memory tracker

GO only if `PHASE3_EXIT_CRITERIA.md` is fully satisfied: one-read capability required, bounded/exact reads, no fallback ladder, stale/oversize/failure cannot mutate tracker, one-current-state only, no persistence/clipboard/network/UI body sink.
