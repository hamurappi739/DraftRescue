# DraftRescue Agent Rules

This file applies to Cursor and any other coding agent working in this repository.

## Canonical source

`docs/DRAFTRESCUE_MASTER_CONTEXT.md` is the canonical product context until the user explicitly changes a requirement.


## Mandatory pre-read

Before changing code, read `docs/CURSOR_HANDOFF_INDEX.md` and the documents it marks as relevant to the requested phase. Use `docs/IMPLEMENTATION_WORK_PACKAGES.md` when a task references a `WP-*` identifier.

Do not silently resolve items in `docs/OPEN_DECISIONS.md`.

## Operating rule

Work on one requested phase/subtask only. Never expand scope on your own. Stop after the requested task and report exactly what changed.

## Absolute privacy rules

1. Do not build a general-purpose keylogger or full typing-history system.
2. Do not store password, PIN, credential, security-code, banking, or other secure-input content.
3. Do not persist content from private/incognito browser contexts by default.
4. Do not send captured text to servers, cloud services, telemetry systems, or AI services.
5. Do not log draft contents, clipboard contents, field contents, or captured user input.
6. When secure/private classification is uncertain, fail closed: do not persist.
7. Keep Win32 and UI Automation code inside `DraftRescue.Platform.Windows`.
8. Keep persistence implementation inside `DraftRescue.Infrastructure`.
9. Domain and Application must remain independent from Avalonia, Win32, UI Automation, and storage implementations.
10. Every implementation task ends with restore/build/tests and a changed-file list.

## Forbidden shortcuts

- Do not add keyboard hooks unless a later explicitly approved phase requires a narrowly justified mechanism.
- Do not create a global keystroke event stream “for convenience”.
- Do not create logs containing raw UIA values or window text that may contain user content.
- Do not implement cloud sync, AI, analytics, mobile support, full history UI, or broad application support early.
- Do not swallow security uncertainty by defaulting to capture.

## Definition of done for any coding task

- Requested scope only.
- Build succeeds.
- Relevant tests pass.
- Privacy rules remain true.
- No sensitive logging.
- Changed files listed.
- Known risks and unresolved issues reported.

## Privacy invariant IDs

Every non-trivial implementation task must identify the relevant invariant IDs from `docs/PRIVACY_INVARIANTS_CATALOG.md` and add/maintain tests for them where practical.

## Accepted decision discipline

Read `docs/ADR_DECISION_SUMMARY_V1.md`. Do not replace accepted decisions (keyed fingerprints, SQLite current-state storage, DPAPI CurrentUser v1, allowed-field capability, context-generation race control) inside an unrelated task. If implementation evidence reveals a blocker, stop and propose a focused ADR change.

## Additional accepted implementation rules

- Recovery list is metadata-only; do not decrypt all drafts to show snippets.
- Recovery matching is categorical and predicate-based (`NoMatch`, `Ambiguous`, `StrongMatch`); do not introduce a global confidence percentage.
- Persist `SnapshotSequence`; repository must reject stale writes inside its transaction.
- Profile resolution ambiguity/unknown target version fails closed.
- Restore is single-flight per `DraftId`; duplicate UI clicks must not inject twice.
- Use stable `DR-*` error codes and audited structural diagnostic fields; never blindly log third-party exception messages.
- Read `USE_CASE_CONTRACTS_V1.md`, `TARGET_ADAPTER_CONTRACTS.md`, and `ERROR_TAXONOMY_AND_CODES.md` before implementing application/platform handlers.

## Phase 1 hard boundary

- Read `docs/PHASE1_PREIMPLEMENTATION_PACKAGE_INDEX.md` before Phase 1 work.
- Phase 1 has zero target-content reads; do not wire a content-reader dependency.
- WinEvent/UIA callbacks enqueue content-free bounded envelopes only.
- Generic UIA cache/query code must follow `UIA_PROPERTY_SAFETY_CLASSIFICATION.md`; do not request Name/HelpText/ItemStatus or text/value properties for convenience.
- An editable control with `IsPassword=false` is not automatically allowed.
- Browser forms remain unsupported for capture until browser/version certification proves private-mode and sensitive-purpose exclusion before read.
- Notepad reconnaissance remains Experimental until later certification gates pass.

## Phase 2 hard boundary

- Read `docs/PHASE2_PREIMPLEMENTATION_PACKAGE_INDEX.md` before any SecureInputGuard work.
- Phase 2 still performs zero target-content reads and contains no concrete `IEligibleFieldTextReader` implementation.
- `AllowedFieldHandle` is a one-read, non-serializable, generation/binding/profile-bound capability with a 1000 ms default monotonic max age and 2000 ms absolute cap.
- Every content-read attempt in Phase 3+ requires a fresh capability; do not cache ambient "safe process/field" permission.
- `IsPassword=false` and `Editable=true` are not positive permission.
- Required security states `Unknown`, `Unavailable`, `Failed`, stale, timeout, profile ambiguity, or unknown target version fail closed.
- Capture capability is `ReadSnapshot` only and can never authorize Restore.
- Security policy evaluation must remain deterministic/pure after structural signal collection.
- Security DI graph must not depend on reader/tracker/repository/protector/clipboard/restore services.

## Phase 3 content-read rule

When Phase 3 is active, read `docs/PHASE3_PREIMPLEMENTATION_PACKAGE_INDEX.md` first. Target content may be read only through a freshly claimed one-read `AllowedFieldHandle`. Production `TextPattern.GetText(-1)`, generic LegacyIAccessible/keyboard/clipboard fallbacks, silent truncation, text normalization, persistence, and content-bearing logs are forbidden. Stop before Phase 4.


## Phase 4 encrypted-persistence lock

When Phase 4 is eventually activated, begin with `docs/PHASE4_PREIMPLEMENTATION_PACKAGE_INDEX.md`. The repository must never accept plaintext; protection happens before persistence. Use DPAPI CurrentUser v1, the canonical SQLite schema/policy, one serialized writer and the monotonic SnapshotSequence transaction rules. Do not enable WAL, LocalMachine DPAPI, plaintext fallback, corruption salvage, Preview, Copy or Restore inside a Phase-4 work package.
