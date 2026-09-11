# Codex Remaining Work and Test Plan

## Executive status

Design/specification is deepest through **Phase 4**. Production implementation is still effectively at **Phase 0 skeleton**.

Therefore the main remaining work is not more brainstorming; it is disciplined implementation and Windows validation in phase order.

## Remaining implementation work

### Phase 0

- verify restore/build/tests on Windows .NET 8;
- launch Avalonia shell;
- repair only skeleton/build issues.

### Phase 1

- WinEvent foreground observation;
- UIA MTA worker;
- safe metadata allowlist/cache requests;
- `TargetFieldCandidateMetadata` normalization;
- event dedupe/coalescing/latest-context reconciliation;
- cancellation/backoff/resource cleanup;
- real Notepad reconnaissance;
- record experiment evidence and numeric budgets.

No target text reads.

### Phase 2

- typed security evidence collection;
- pure fail-closed policy evaluator;
- hard protected/password deny;
- controlled credential/PIN/payment negative fixtures;
- version/profile-bound positive allow predicates;
- one-read capability issuer/registry;
- capability expiry/revocation/atomic claim;
- architecture/source guards proving denied candidates cannot reach content reader.

### Phase 3

Completed in the current bounded block:

- bounded certified TextPattern reader;
- certified bounded ValuePattern reader for explicitly certified single-line surfaces;
- one-shot capability consumption;
- typed read outcomes;
- exact text preservation and explicit oversize failure;
- snapshot generation/sequence race envelope;
- one-current-state in-memory tracker;
- stable empty/clear semantics;
- architecture/privacy/content-canary tests.

Phase 3 is now complete: integrated reader fault/non-leakage evidence, the
30-minute operational soak and the final exit gate all pass. The next bounded
work package is Phase 4 encrypted local persistence.

### Phase 4

- `IDraftProtector` + DPAPI CurrentUser implementation;
- DPAPI-protected installation HMAC secret;
- SQLite schema/bootstrap/PRAGMA verification;
- protected-record-only repository;
- single writer coordinator;
- monotonic transaction semantics;
- scheduler -> protector -> repository pipeline;
- retention/expiry execution;
- corruption/quarantine/migration behavior;
- crash/disk-full/lock/stale-sequence fault injection;
- plaintext-at-rest canary certification.

### Phase 5

- metadata-only recoverable list;
- empty/loading/error/ready UI states;
- explicit Preview decrypt lifecycle;
- Discard and Discard All;
- Copy after clipboard boundary review;
- tray/background interaction;
- accessibility/keyboard navigation;
- no body decryption on list load.

### Phase 6

- recovery evidence policy implementation;
- live target discovery/rebinding;
- fresh security recheck;
- first direct restore adapter;
- write verification;
- race/single-flight handling;
- lifecycle cleanup only after verified success.

### Phase 7

- Chrome synthetic research;
- Incognito/private detection proof before capture;
- one certified normal Chrome editor surface;
- negative login/payment/browser-chrome cases;
- cross-origin/unknown-version safety;
- repeat independently for Edge.

### Phase 8

- Discord research/profile;
- login/credential negatives;
- composer tracking/completion;
- recovery/restore;
- update/version safety.

### Phase 9

- move target-specific behavior behind stable profile contracts;
- finalize resolver/version compatibility;
- per-profile certification matrices;
- ensure profiles cannot weaken global denies.

### Phase 10

- full threat/invariant/test audit;
- privacy-negative suite;
- crash/restart/retention/race suite;
- resource/leak/soak suite;
- logging/storage canary audit;
- packaging/startup/update tests;
- release privacy checklist;
- unsupported-case documentation.

## Global test execution order

For each work package:

1. run focused unit/architecture tests;
2. run the named phase test IDs;
3. run applicable privacy/correctness regression tests;
4. run full test suite before phase gate;
5. run manual/Windows experiment steps where the spec requires real OS evidence;
6. record experiment result using `specs/experiment-result.schema.json` where applicable.

## Canonical test registry

`specs/test-catalog.v1.json` currently contains **229 test cases**.

Never treat this file as optional documentation. It is the canonical machine-readable registry.

## Mandatory negative testing philosophy

Positive functionality is insufficient.

For every target/app capability, prove negatives first or alongside positives:

- password/protected;
- PIN/OTP;
- payment/banking;
- private/incognito;
- unsupported profile;
- unknown version;
- stale target;
- provider failure/timeout;
- integrity/elevation mismatch;
- ambiguous recovery target;
- stale/out-of-order snapshot;
- DPAPI failure;
- storage corruption/lock/disk full;
- restore target change.

Whenever possible assert both the user-visible result and the **absence of forbidden side effects** such as:

```text
TextReader.InvocationCount == 0
Repository.WriteCount == 0
Protector.InvocationCount == 0
Clipboard.WriteCount == 0
Network.SendCount == 0
```

## Release blockers

The product is not release-ready if any of these are true:

- sensitive/private field can be read or persisted;
- unknown security state can become Allow;
- target content appears in logs/telemetry/network;
- plaintext canary appears in DraftRescue durable artifacts;
- Recovery list decrypts bodies automatically;
- stale async result can overwrite newer state;
- revision/history data accumulates;
- Restore can bypass fresh security/match checks;
- browser private-mode coverage is uncertain but normal browser capture is enabled;
- unknown app version inherits Supported behavior;
- phase acceptance gate is not completely green.
