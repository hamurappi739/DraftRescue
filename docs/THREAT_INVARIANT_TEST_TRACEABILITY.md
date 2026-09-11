# Threat -> Invariant -> Test Traceability Matrix

**Status:** release-gating map. Every critical threat must map to enforceable invariants and tests before MVP release.

| Threat / failure | Primary invariants | Minimum tests / evidence |
|---|---|---|
| Architecture becomes a keylogger | P-001, P-002 | architecture scan; OBS tests; code review of platform observer |
| Password/credential capture | P-002, P-003, P-004, P-005 | SEC password/PIN/payment fixtures; classifier-before-read assertions |
| Private browsing capture | P-003, P-006 | BRW private-mode matrix for every supported browser/version |
| Text leaves device | P-007 | NET-001; runtime dependency/network inspection |
| Plaintext leaks to logs/support bundles | P-008, P-020 | LOG-001..004 with unique canaries |
| Plaintext remains at rest | P-009 | PST-001, PST-004, DB/raw-file inspection |
| Product becomes typing history | P-010 | PST-002; schema review forbidding revision table |
| Drafts persist indefinitely | P-011, P-018 | retention boundary tests; startup expiry tests |
| URL/title metadata becomes browsing history | P-012 | PST-009; LOG-003; fingerprint source-string inspection |
| Restore writes into wrong/new target | P-013, P-014 | RST-001..004; generation/race tests |
| Restore overwrites unrelated content | P-013, P-014 | non-empty target rejection; profile certification |
| Clipboard exposure without intent | P-015 | UI-004; RST clipboard test |
| UI decrypts entire database | P-016 | PST-003; UI-002/003 |
| Unknown app becomes permissive | P-017 | profile resolver tests; unknown executable deny |
| Expired record reappears | P-018 | PST-007 plus crash/restart expiry scenario |
| Tests use real secrets/user data | P-019 | fixture repository review; generated canary-only harness |
| Race allows old async read to overwrite new context | P-003, P-013 | TRK-005; ContextGeneration stress test |
| Update/migration creates plaintext temp data | P-009 | packaging migration test and disk canary scan |
| Elevated/secure desktop fallback weakens isolation | P-003, P-017 | elevation/secure-desktop capability tests |
| Shutdown path hangs/logoff is blocked | P-003 | bounded session-end tests; UIA-hang during shutdown |
| Physical duplicate rows become duplicate history | P-010 | recovery deduplication tests |

| Event callback/experiment leaks UI content | P-025, P-026, P-028, P-029 | OBS-004; SEC-009; EXP-001/002 |
| Event storm retains/promotes stale context | P-003, P-025 | OBS-006/007; SEC-011 |
| Broad UIA cache fetches sensitive strings before classifier | P-002, P-026 | SEC-009; PERF-004 |
| Generic browser edit field captures credential/payment/private data | P-005, P-006, P-027 | SEC-010; BRW matrix; CERT-002 |
| Research/certification artifacts contain real user data | P-019, P-029, P-030 | EXP-001; FI-004; fixture audit |

| Reusable/stale capture permission reads later-sensitive content | P-002, P-031, P-032, P-034 | CAP-001..005; SEC-015/016 |
| Security layer accidentally calls content reader directly | P-002, P-035 | ARC-005, ARC-007, P2F-001..004 |
| `IsPassword=false` or editability becomes permissive default | P-004, P-005, P-036 | SEC-017, SEC-018, SEC-019/020 |
| Capture permission reused for Restore write | P-013, P-033 | CAP-006; RST-001 |

## Release rule

A critical row cannot be marked "Not Applicable" merely because the implementation skipped its tests. It is N/A only when architecture makes the threat impossible and that claim is itself reviewed/tested.

## Traceability maintenance

When adding a new invariant or threat:

1. allocate stable ID where appropriate;
2. update machine-readable invariant catalog if privacy/security relevant;
3. add at least one automated or explicit manual verification;
4. add the mapping here;
5. update release privacy checklist.
