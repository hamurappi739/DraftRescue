# Phase 2 gate compatibility review — 2026-09-12

## Finding

The Phase 2 content-boundary guard was still scanning the complete `DraftRescue.Platform.Windows` tree after Phase 3 introduced the authorized `CertifiedReadTarget`/reader path. That made the historical Phase 2 gate report `Inconclusive` on a valid Phase 3 repository because it matched `DocumentRange.GetText` outside the Phase 2 security boundary.

## Correction

The guard now scans only:

- `src/DraftRescue.Application/Security`;
- `src/DraftRescue.Platform.Windows/Security`.

The Phase 3 content-boundary guard remains responsible for the authorized reader surface and continues to reject unbounded reads, clipboard/keyboard fallbacks, persistence, and restore dependencies in the Phase 3 scope. Its source roots are now limited to the Phase 3 application models/drafts and Windows Automation/Observation/Reading/Reliability components, excluding the Phase 4 Security/Storage adapters. The Phase 2 gate still rejects all content APIs, clipboard APIs, and keyboard hooks within the security components.

## Privacy and correctness

This is a test-boundary correction only. It does not loosen the security policy, issue capabilities, or permit any new target. Relevant invariants are P-002, P-003, P-004, P-005, P-006, P-031, P-032, P-033, P-034, P-035, P-036, P-037 and C-019/C-020/C-026/C-027/C-028/C-029/C-030/C-031/C-032/C-033.

## Verification

- Full test suite: **167/167** passed.
- Phase 2 content-boundary guard: Pass after the scope correction.
- Phase 3 content-boundary guard: remains Pass.
- No target-content fixture, clipboard access, keyboard hook, or logging path was added.
