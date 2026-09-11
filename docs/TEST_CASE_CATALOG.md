# Test Case Catalog

**Purpose:** stable test IDs for future implementation. These are acceptance/privacy obligations, not suggestions.

## Architecture

### ARC-001 Domain has no platform/UI/storage dependency
- Phase: 0+
- Invariants: P-016
- Pass: Domain references no Avalonia/Win32/UIA/SQLite/DPAPI implementation package.

### ARC-002 Application has no Windows implementation dependency
- Phase: 0+
- Pass: Application depends on abstractions/domain only.

### ARC-003 Repository API cannot accept plaintext
- Phase: 4
- Invariants: P-009
- Pass: public repository surface accepts protected record/payload only.

## Observation / gating

### OBS-001 Foreground event contains no text
- Phase: 1
- Invariants: P-001, P-002

### OBS-002 Own-process events ignored
- Phase: 1
- Pass: DraftRescue UI interaction does not become capture candidate.

### OBS-003 Stale context result discarded
- Phase: 1/2/3
- Invariants: P-002, P-003
- Setup: delay metadata/read for generation N; switch to N+1.
- Pass: N result is not tracked/persisted.

### SEC-001 IsPassword true never reads text
- Phase: 2
- Invariants: P-002, P-004
- Pass: text-reader spy invocation count remains zero.

### SEC-002 Security provider timeout never reads text
- Phase: 2
- Invariants: P-002, P-003

### SEC-003 Unknown app/profile fails closed
- Phase: 2/9
- Invariants: P-003, P-017

### SEC-004 Synthetic credential field denied
- Phase: 2
- Invariants: P-005

### SEC-005 Synthetic payment/card field denied
- Phase: 2
- Invariants: P-005

## Browser privacy

### BRW-001 Normal browser window can continue to field classification
- Phase: 7
- Pass: does not imply field Allowed automatically.

### BRW-002 Private window denied before text read
- Phase: 7
- Invariants: P-002, P-006

### BRW-003 Normal and private windows coexist correctly
- Phase: 7
- Invariants: P-006
- Pass: classification is window/context-specific, not process-global.

### BRW-004 Unknown private-mode state denies
- Phase: 7
- Invariants: P-003, P-006

### BRW-005 Browser chrome/address bar unsupported for MVP
- Phase: 7
- Invariants: P-017

## Tracking

### TRK-001 Hundred updates produce one logical draft
- Phase: 3/4
- Invariants: P-010

### TRK-002 Focus loss does not delete draft
- Phase: 3

### TRK-003 Stable explicit clear removes obsolete draft
- Phase: 3

### TRK-004 Unknown completion keeps draft until retention
- Phase: 3

### TRK-005 Older async snapshot cannot overwrite newer snapshot
- Phase: 3/4

## Persistence / crypto

### PST-001 Raw DB bytes contain no known plaintext canary
- Phase: 4
- Invariants: P-009

### PST-002 Repeated updates do not append revision rows
- Phase: 4
- Invariants: P-010

### PST-003 Startup list does not decrypt every payload
- Phase: 4/5
- Invariants: P-016

### PST-004 DPAPI protection failure has no plaintext fallback
- Phase: 4
- Invariants: P-003, P-009

### PST-005 Unknown protection version fails closed
- Phase: 4

### PST-006 Crash during update leaves old or new valid record
- Phase: 4

### PST-007 Expired record excluded even before physical cleanup
- Phase: 4
- Invariants: P-011, P-018

### PST-008 Discard-all does not decrypt records
- Phase: 4/5

### PST-009 Fingerprint source strings absent from DB fixture
- Phase: 4
- Invariants: P-012

## UI / preview / copy

### UI-001 Empty state contains no history framing
- Phase: 5

### UI-002 List query returns metadata without plaintext body
- Phase: 5
- Invariants: P-016

### UI-003 Preview decrypts only selected draft
- Phase: 5

### UI-004 Copy requires explicit user action
- Phase: 5
- Invariants: P-015

### UI-005 Discard removes card and record
- Phase: 5

## Recovery / restore

### RST-001 Restore always performs fresh security classification
- Phase: 6
- Invariants: P-013

### RST-002 Ambiguous match offers no direct Restore
- Phase: 6
- Invariants: P-014

### RST-003 Target changes after match abort write
- Phase: 6
- Invariants: P-013

### RST-004 Target becomes secure after click aborts
- Phase: 6
- Invariants: P-013

### RST-005 Confirmed restore removes recoverable payload
- Phase: 6

### RST-006 Failed restore keeps recoverable draft
- Phase: 6

### RST-007 Unsupported write capability is Copy-only
- Phase: 6

## Logging / diagnostics

### LOG-001 Draft canary absent from logs
- Phase: 2+
- Invariants: P-008

### LOG-002 Protected payload bytes absent from logs
- Phase: 4+
- Invariants: P-008

### LOG-003 Full URL/title absent from default structural logs
- Phase: 7+
- Invariants: P-012

### LOG-004 Future support bundle contains no payload/content
- Phase: 10+
- Invariants: P-020

## Network

### NET-001 No runtime network dependency for draft path
- Phase: all MVP
- Invariants: P-007
- Pass: capture/persistence/recovery works offline and no text-bearing network client exists in path.

## Soak / performance

### PERF-001 Focus switch soak has bounded handles
- Phase: 1/10

### PERF-002 No hot polling while idle
- Phase: 1/10

### PERF-003 Hung UIA provider triggers bounded timeout/backoff
- Phase: 1/10
- Invariants: P-003


## Phase 4 extended persistence certification

### PST-010 Repository public API has no plaintext parameter
- Phase: 4

### PST-011 DPAPI uses CurrentUser, never LocalMachine
- Phase: 4

### PST-012 Installation HMAC secret is random and DPAPI-protected
- Phase: 4

### PST-013 Missing secret with existing DB does not silently regenerate identity
- Phase: 4

### PST-014 Metadata list query does not select protected body
- Phase: 4

### PST-015 Rollback journal DELETE mode is verified at runtime
- Phase: 4

### PST-016 secure_delete=ON is verified at runtime
- Phase: 4

### PST-017 synchronous=EXTRA baseline is verified at runtime
- Phase: 4

### PST-018 Equal snapshot sequence with contradictory record fails typed
- Phase: 4

### PST-019 Corrupt store enters fail-closed quarantine/unavailable path without salvage
- Phase: 4

### PST-020 Plaintext canary absent from all DraftRescue-owned durable artifacts after update/delete/crash tests
- Phase: 4

## Phase 4 fault injection

### P4F-001 Phase-4 fault scenario 01
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-002 Phase-4 fault scenario 02
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-003 Phase-4 fault scenario 03
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-004 Phase-4 fault scenario 04
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-005 Phase-4 fault scenario 05
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-006 Phase-4 fault scenario 06
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-007 Phase-4 fault scenario 07
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-008 Phase-4 fault scenario 08
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-009 Phase-4 fault scenario 09
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-010 Phase-4 fault scenario 10
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-011 Phase-4 fault scenario 11
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-012 Phase-4 fault scenario 12
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-013 Phase-4 fault scenario 13
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-014 Phase-4 fault scenario 14
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-015 Phase-4 fault scenario 15
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-016 Phase-4 fault scenario 16
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-017 Phase-4 fault scenario 17
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-018 Phase-4 fault scenario 18
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-019 Phase-4 fault scenario 19
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.

### P4F-020 Phase-4 fault scenario 20
- Phase: 4
- Normative expected behavior: `PHASE4_FAULT_INJECTION_MATRIX.md`.



## Registry synchronization note

`../specs/test-catalog.v1.json` is the machine-readable canonical ID registry and includes test IDs introduced by phase-specific traceability documents even when their full scenario definition remains in that phase document. An ID referenced by an invariant/exit gate must exist in the JSON registry.
