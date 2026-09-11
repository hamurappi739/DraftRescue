# Phase 1 Provider-Stress Review — 2026-09-04

## Decision

**Provider-stress review: Pass. Phase 1 exit gate: Pass (8/8).**

The real UI Automation metadata worker is bounded and content-free under a
reconciliation storm. Automated WPF repeatability remains `0/5` in the
non-interactive runner, but the bounded interactive confirmation passed with
fixture-focused metadata and no content access. The formal WPF focus-delivery
criterion is therefore satisfied by the interactive evidence path; the
automated result remains an environment diagnostic, not a provider crash or a
permissive capture defect.

Phase 2 may proceed to its preimplementation package review. The automated
repeat limitation must remain documented and must not be bypassed by weakening
privacy or focus checks.

## Scope boundary

This review covers only the Phase 1 observation plane:

- foreground WinEvent envelopes;
- UI Automation focused-element structural metadata;
- bounded reconciliation and provider failure backoff;
- lifecycle/disposal behavior;
- metadata-only experiment records.

It does not authorize text reads, clipboard access, keyboard hooks, persistence,
restore, browser support, cloud calls, telemetry, or Phase 2 security policy.

## Evidence matrix

| Area | Evidence | Result |
|---|---|---|
| Provider storm | `artifacts/phase1-provider-stress/WP14-PROVIDER-STRESS.json` | Pass: 2000 requests, 2001 signals, 2 reads, 0 provider failures, 0 text/repository calls |
| Failure backoff | `ProviderFailureBackoffTests` and `UiaFocusedElementMetadataSourceTests.RepeatedProviderFailures_UseBoundedBackoff` | Pass: 250/500/1000/2000/4000/5000 ms cap sequence and recovery |
| Hung provider | `artifacts/phase1-hang-evidence/WP14-HANG.json` | Pass: bounded dispose and single release |
| Synthetic routing | `artifacts/phase1/WP14-SYNTH.json` | Pass: duplicate coalescing, newest context, warmed lifecycle |
| Long soak | `artifacts/phase1-long/WP14-SOAK.json` | Pass: 1800-second synthetic soak; retained as non-certifying measurement |
| Desktop shell | `artifacts/phase1-desktop-ui/WP14-UI.json` | Pass: no non-responding samples in bounded shell probe |
| WPF repeat | `artifacts/phase1-wpf-repeat-suite/WP14-WPF-REPEAT-AUTO.json` | Inconclusive: 0/5, all attempts `ActivationBlocked` |
| Interactive WPF confirmation | `artifacts/phase1-wpf-interactive-review/WP14-WPF-INTERACTIVE-REVIEW.json` | Pass: fixture-focused metadata, 3 foreground observations, 1 focused metadata observation, 0 provider failures, 0 content reads |

## Latest WPF blocker telemetry

The selected automated summary records:

- fixture foreground observations: `2`;
- Win32 activation attempts: `95`;
- Win32 activation successes: `0`;
- UIA `SetFocus` calls: `5`;
- UIA focus-call returns: `5`;
- verified foreground activations: `0`;
- focused metadata observations for fixture: `0`;
- non-null structural UIA reads: `5`;
- provider failures: `0`;
- diagnosis: `ActivationBlocked`.

The distinction matters: UIA metadata reads are functioning, but the test
environment does not grant the fixture a verifiable interactive foreground.
The gate therefore remains fail-closed and does not convert this result into a
support claim.

## Privacy and correctness review

Relevant invariants: P-001, P-002, P-003, P-026, P-028, P-029, P-030,
C-003, C-016, C-017, C-018, C-022, C-023, C-025.

The latest suite and provider-stress records confirm:

- `rawTargetTextCaptured=false`;
- `rawDynamicUiaStringsLogged=false`;
- `clipboardRead=false` and `clipboardWritten=false`;
- `networkTextSent=false`;
- text-reader invocations: `0`;
- repository invocations: `0`;
- target content read: `false`.

The UIA activation helper is test-only and operates on synthetic window handles;
production observation code never activates or focuses a user's window.

## Reproduction commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_provider_stress.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\run_phase1_evidence_suite.ps1 -IncludeNotepad -SoakSeconds 30 -UiDurationSeconds 10 -WpfRepeatCount 5
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\phase1_exit_gate.ps1
```

Expected current result: provider stress and all required suite steps pass;
automated WPF repeat remains `0/5`, while the formal gate passes `8/8` using
the validated interactive confirmation record.

The latest integrated run is
`artifacts/phase1-suite-final5/PHASE1-EVIDENCE-SUITE.json`:
`11/11` required steps passed, with only the automated WPF repeat retained as
an expected inconclusive diagnostic. The
`PHASE1-EVIDENCE-PACKAGE-GUARD.json` record passes its eight-record integrity
and privacy audit and confirms zero text-reader/repository invocations.

The content-free interactive review at
`artifacts/phase1-wpf-interactive-review/WP14-WPF-INTERACTIVE-REVIEW.json`
reports a passing bounded probe: `FixtureFocused`, three foreground
observations, one focused metadata observation, zero provider failures, and
zero text/repository calls. The advisory preflight remains non-authorizing;
the fixture itself becoming foreground is what supplies the confirmation.

The suite runner now invokes the already-built harness executable directly and
cleans only child processes created during the current run. The latest suite
record captured zero known processes at baseline, cleaned 5 owned helper
processes, and observed zero owned processes after the final drain. This keeps
the process assertion scoped to the current run and prevents stale workers
from invalidating later builds.

## Required next action

Read `docs/PHASE2_PREIMPLEMENTATION_PACKAGE_INDEX.md` before beginning any
SecureInputGuard work. Keep the automated WPF `0/5` result as an environment
diagnostic and preserve fail-closed behavior; do not add a global input hook or
text reader to improve the diagnostic repeat.
