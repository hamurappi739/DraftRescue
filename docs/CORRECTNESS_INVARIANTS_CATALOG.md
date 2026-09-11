# Correctness Invariants Catalog

These invariants complement privacy IDs `P-*`. They are stable references for implementation tests and Cursor work packages.

## C-001 Monotonic snapshot state

For one `DraftId`, an older `SnapshotSequence` never replaces a newer current state.

## C-002 One current durable row

Repeated edits update one logical current record; they do not create revision history.

## C-003 Stale context isolation

A result from context generation N cannot mutate state belonging to generation N+1.

## C-004 Deterministic profile resolution

The same normalized live application identity/profile set produces the same resolution; overlapping eligible profiles return ambiguity rather than first-match behavior.

## C-005 Deterministic recovery predicate

`StrongMatch` is produced only by the profile's explicit evidence predicate. Weak evidence/order does not produce a strong result accidentally.

## C-006 Single-flight restore

At most one restore mutation is in flight for a given `DraftId` inside the primary process.

## C-007 Idempotent discard

Repeated `DiscardDraft` for an already-absent draft does not fail dangerously or recreate state.

## C-008 Copy is non-terminal

Successful Copy does not remove or mark the recoverable record terminal.

## C-009 Verified-restore deletion rule

The sole recovery record is automatically removed only after verified restore success or another explicit terminal lifecycle rule. Applied-but-unverified keeps the copy.

## C-010 Metadata list is body-free

Listing recoverable drafts does not decrypt draft bodies or construct plaintext snippets.

## C-011 Expiry cannot revive

Once a record is expired at a trusted application-time decision, later wall-clock movement does not intentionally resurrect it into the same running session.

## C-012 Transactional current state

A committed protected record contains mutually consistent payload, fingerprints, sequence, and timestamps from one snapshot state.

## C-013 Command result honesty

UI success states correspond to typed application results; exceptions/timeouts cannot be translated into success by presentation code.

## C-014 Duplicate UI activation is safe

Double-click/repeated activation of Restore/Discard/Copy cannot create duplicate Restore injection or contradictory terminal state.

## C-015 Unknown target version is not supported

A target outside certified compatibility is not silently treated as the nearest known supported version.

## C-016 Latest-context convergence

After duplicate/bursty/out-of-order observation events settle, reconciliation converges on the currently focused logical context rather than replaying an event history.

## C-017 Deterministic candidate normalization

Equivalent platform metadata normalizes to the same semantic candidate independent of event/property ordering.

## C-018 Callback boundedness

Observation callbacks perform only bounded enqueue/signaling work and never execute deep provider traversal or synchronous persistence/UI work.

## C-019 Deny precedence is order-independent

Hard-deny security evidence cannot be bypassed by evaluation order or positive allow hints.

## C-020 Allowed capability is generation-bound

An `AllowedFieldHandle` issued for context generation N is invalid in N+1 and cannot authorize a later read/restore.

## C-021 Experiment artifact validity

A Phase 1 experiment result is valid only when its content-safety audit proves no forbidden content collection occurred.

## C-022 Candidate metadata is bounded

Normalized candidate metadata has explicit property/string/collection bounds and cannot grow with document/accessibility-tree size.

## C-023 Backpressure never promotes stale context

Queue saturation/coalescing can discard intermediate observations but cannot cause an older context to become current or retain an old allow capability.

## C-024 Missing negative evidence blocks certification

A target/version cannot be promoted to Supported when required privacy-negative cases are missing, inconclusive, or skipped.

## C-025 Provider failure remains typed and bounded

UIA/provider failure, timeout, or stale element produces a typed non-success result and bounded retry/backoff; it never silently becomes successful metadata.

## C-026 Capability single-consumption

For one `AllowedFieldHandle`, at most one atomic claim succeeds. Concurrent or sequential duplicate claims fail.

## C-027 Capability deadline uses monotonic time

Capability age/expiry decisions do not depend on wall-clock jumps; tests can drive them with a fake monotonic clock.

## C-028 Security policy determinism

The same normalized `SecurityEvidenceSet` produces the same decision/reason independent of signal collection order.

## C-029 Capability issuance rechecks current binding

An Allowed policy result cannot produce a capability if candidate binding/generation/profile evidence is no longer current at issuance.

## C-030 Phase 2 zero content reads

Every Phase 2 production/test path except explicit fake-spy accounting performs zero actual target-content reads.

## C-031 Canonical deny reason priority

When multiple hard-deny conditions coexist, the emitted diagnostic reason is selected by the stable documented priority without changing deny truth.

## C-032 Capability registry is bounded

Consumed/revoked capability state does not become a durable or unbounded in-memory history; entries expire/are removed under bounded lifecycle rules.
## C-033 Reader requires claimed capability

The content reader cannot be invoked successfully using metadata/policy output alone; an atomically claimed capability is mandatory.

## C-034 Deterministic read-strategy selection

The same certified profile/capability selects the same content-read strategy; provider failure does not probe an opportunistic fallback chain.

## C-035 Oversize result is explicit

A read exceeding its configured complete-snapshot limit returns `TooLarge`; it cannot be accepted as a truncated success.

## C-036 Empty differs from failure

Empty text is a successful content state. Timeout, exception, unsupported strategy, cancellation, and stale target are never coerced to empty.

## C-037 Read result is generation/sequence bound

A snapshot from an older generation/sequence cannot mutate a newer current draft even if provider completion arrives later.

## C-038 In-memory tracker is monotonic current state

For one logical draft, accepted snapshot sequence advances monotonically and there is only one current text value.

## C-039 Capture is exact/idempotent

Re-reading unchanged target text yields exact ordinal equality and does not create a revision or transformed variant.

## C-040 Plaintext ownership remains bounded

Superseded snapshot references are released; the capture pipeline does not retain an unbounded collection of plaintext values.


## C-041 Persisted sequence is final stale-write authority

Repository transaction comparison prevents an older already-protected record from overwriting a newer committed sequence.

## C-042 Protect success is not commit success

A successful DPAPI operation does not advance durable/committed state until the SQLite transaction commits.

## C-043 Equal-sequence contradiction is not silently accepted

Same DraftId+SnapshotSequence with contradictory record content returns a typed conflict/corruption result.

## C-044 SQLite policy is verified, not assumed

Phase 4 reads back/validates required journal/synchronous/secure-delete policy rather than relying on provider defaults.

## C-045 Expiry visibility is independent of cleanup success

Expired records remain hidden even when physical delete is delayed/fails.

## C-046 Existing store prevents silent fingerprint identity reset

Missing/corrupt installation secret with existing rows does not silently generate a replacement identity.

## C-047 Known migrations are bounded and fail-closed

Supported migrations are explicit and transactional where possible; failure does not auto-drop the recovery store.

## C-048 Persistence writer remains serialized and bounded

Concurrent checkpoint requests do not create unbounded DB writers/retry loops.

## C-049 Plaintext-at-rest certification is reproducible

Synthetic canary scans cover DraftRescue-owned durable artifacts across update/delete/crash boundaries.

## C-050 Corruption handling preserves truthful state

A corrupt/quarantined store is reported unavailable rather than silently rebuilt and reported recovered.
