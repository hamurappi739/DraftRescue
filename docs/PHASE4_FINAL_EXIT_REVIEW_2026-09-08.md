# Phase 4 WP4.8 Final Exit Review — 2026-09-08

## Decision

The final Phase-4 evidence combiner was executed. Prior WP4.x gates, the source boundary, and the 181-test suite are green, but the Phase-4 exit remains **Inconclusive** rather than being marked complete.

## Green evidence

- WP4.1–WP4.7 gate artifacts are present and report Pass.
- Full Debug build: 0 warnings, 0 errors.
- Full test suite: 181/181.
- Plaintext/fallback source boundary: no WAL, broad decrypted enumeration, file-based plaintext writes, network, or Preview/Copy/Restore implementation in the Phase-4 paths.
- Synthetic canary, rollback, one-row, corruption/quarantine, and metadata-only evidence is preserved.
- `PHASE4-PROCESS-KILL-PROBE.json` now passes: child process killed during an open transaction, previous committed row verified after restart.
- Deterministic `BeforeCommit` disk-full injection now passes: transaction abort leaves the previous protected row intact.
- `PHASE4-TARGET-ENVIRONMENT-PROBE.json` confirms the temp volume is writable, but no disposable quota/virtual-disk fixture is configured for real disk-full injection.

## Explicit blockers

1. The compiled `DraftRescue.Phase4CrashProbe` now performs the DPAPI `CurrentUser` roundtrip directly (no reflection loader), and its argument-validation bug was fixed. The current test host still reports `available=false` / typed `DpapiFailure`; the positive Windows profile probe must be rerun on a target host where DPAPI is available.
2. The real process-kill probe now passes: the child is killed during an uncommitted SQLite transaction and the previous row is recovered. Real disk-full injection remains the only fault-certification task requiring a target environment with a controlled quota/virtual disk.

These blockers are recorded in `artifacts/phase4-exit-gate/PHASE4-EXIT-GATE.json` and the repeatable `PHASE4-DPAPI-RUNTIME-PROBE.json`; no plaintext fallback or unsafe recovery path is introduced to hide them.

## Boundary after this review

Do not start Phase 5 Preview, Copy, or Restore workflows until the two blockers are resolved and the exit gate reports Pass. The app shell remains runnable and the Phase-4 implementation remains fail-closed.

## WP4.8 follow-up — 2026-09-12

The exit evidence scripts were hardened without changing the Phase-4 product boundary:

- `PHASE4-TARGET-ENVIRONMENT-PROBE.json` is now schema v2 and records only content-free, typed session/identity/profile/volume capability fields. In this host the session is interactive and the user profile path is present, but the profile load query is unavailable (`loadState=QueryUnavailable`, `loadStateQueryAvailable=false`), so DPAPI readiness remains unproven; the temp volume is writable with approximately 122 GB free and no disposable disk-full fixture.
- `PHASE4-DPAPI-RUNTIME-PROBE.json` is schema v3. The compiled probe checks both the payload and installation-secret DPAPI paths and completed with `probeExitCode=6` (`DpapiFailure`) and `harnessExitCode=0`, so this is a repeatable target-environment result rather than a harness failure; `failureStage` identifies the failed operation without emitting provider text, and the two round-trip booleans plus `containsSecrets=false` remain explicit.
- `PHASE4-EXIT-GATE.json` is schema v2 with dynamic `181/181` test parsing, typed required items and `pendingItems`. The full solution build is `0` warnings / `0` errors, process-kill rollback is Pass, and synthetic `BeforeCommit` rollback plus plaintext canary remain Pass.
- `PHASE4-TARGET-CERTIFICATION.json` now provides a single target-host checklist and explicit next action; on the current host it remains Inconclusive because `profile.loaded=false` and no real disk-full evidence was supplied.
- The main exit combiner accepts an explicit `-DiskFullEvidencePath` and records only a safe artifact label after requiring `RealControlledDiskFull`/`Pass`; synthetic or secret-bearing artifacts cannot promote the gate.
- The privacy contract was exercised with a synthetic/rejected fixture: the gate returned `Fail` and `real-disk-full-evidence-rejected-by-typed-privacy-contract`, then the canonical run was restored to `Inconclusive` with the two expected pending items.

Relevant invariants for this hardening block are P-008, P-019, P-020, P-030, P-046 and C-021/C-049: diagnostics remain content-free, evidence is synthetic/non-sensitive until target certification, absolute paths are not exported, and certification remains reproducible and typed.

The typed disk-full contract now requires explicit `syntheticOnly` and `containsSecrets` properties; omitted fields are rejected instead of being coerced from null to a permissive false value.

The complete WP4.1–WP4.7 regression sequence was rerun after the hardening changes: contracts, DPAPI boundary, SQLite, coordinator, recovery, and plaintext/fault gates all report `Pass` with `181/181` tests. The final WP4.8 combiner remains `Inconclusive` only for the two target-environment certifications.

The profile-state refinement prevents an unavailable WMI query from being misreported as a definitive `NotLoaded` state; both outcomes remain fail-closed.

Target certification also handles missing/malformed external evidence as a typed `Inconclusive` artifact instead of throwing away the run result (`diskFullEvidenceLoadStatus`), preserving an auditable next action.

The handoff consistency guard now reports `Pass` with zero findings: status JSON, exit-gate/target-certification/privacy artifacts, and the 1420-file inventory agree.

The honest aggregate remains **Inconclusive** with exactly two pending target certifications: positive DPAPI CurrentUser roundtrip and real controlled disk-full injection. `phase4Exit=false` and `syntheticEvidenceOnly=true`; no plaintext fallback, Preview, Copy, Restore, or Phase 5 behavior was enabled.
