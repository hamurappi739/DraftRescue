# Use-Case Contracts v1

**Status:** accepted pre-implementation contract. The names may map to handlers/services in code, but the data boundaries and outcomes are normative.

## 1. Rule

Desktop UI invokes purpose-built application use cases. It never talks directly to SQLite, DPAPI, UI Automation objects, app-profile manifests, or a generic decryptor.

Every use case returns a typed result. Exceptions are reserved for programming defects/cancellation boundaries; ordinary platform/storage/product failures map to stable DraftRescue error codes.

No user-facing request type contains arbitrary captured text except the internal transient snapshot path explicitly marked below.

## 2. Observation orchestration

### `HandleForegroundContextChanged`

Input:

```text
ForegroundContextChanged
- ContextGeneration
- ApplicationIdentity
- ProcessId
- TopLevelWindowToken
- ObservedAtUtc
```

Content rule: metadata only.

Outcomes:

```text
IgnoredOwnProcess
UnsupportedApplication
CandidateInspectionScheduled
StaleIgnored
ObservationUnavailable
```

Side effects: may schedule metadata inspection. It must not read field text.

### `InspectFocusedCandidate`

Input: current `ContextGeneration` + metadata-only `ForegroundContext`.

Pipeline:

1. resolve app profile;
2. locate candidate editable field metadata;
3. collect typed metadata-only security evidence;
4. evaluate pure fail-closed security policy;
5. if and only if `Allowed`, revalidate generation/binding/profile and mint a one-read `AllowedFieldHandle`.

Outcomes:

```text
AllowedFieldReady
Denied(CaptureDenialReason)
NoEditableCandidate
StaleIgnored
ProviderUnavailable
```

A denied/provider-error result must never cause a broader fallback capture path.

## 3. Snapshot acquisition

### `AcquireEligibleSnapshot`

Input:

```text
AcquireEligibleSnapshotRequest
- AllowedFieldHandle
- ContextGeneration
- SnapshotSequence
```

Output:

```text
AcquireEligibleSnapshotResult
- Captured(FieldTextSnapshot)   // transient plaintext
- Empty
- StaleIgnored
- HandleExpired
- HandleAlreadyConsumed
- HandleRevoked
- TargetChanged
- ReadUnsupported
- ProviderTimeout
- ProviderFailure
```

The returned plaintext object is process-memory only and is handed directly to tracking/protection orchestration. It is not logged, serialized, cached globally, or returned to normal UI list queries.

## 4. Track current draft

### `ApplyAllowedSnapshot`

Input:

```text
AllowedDraftSnapshot
- DraftId?                     // absent when a new logical draft starts
- ApplicationId
- AppProfileId
- AppProfileVersion
- ContextGeneration
- SnapshotSequence
- ContextFingerprintSet
- DraftPlaintext               // transient
- ObservedAtUtc
```

Outcomes:

```text
Created(DraftId)
Updated(DraftId)
NoMaterialChange(DraftId)
ClearedAndRemoved(DraftId)
StaleIgnored
RejectedByPolicy
ProtectionFailed
PersistenceFailed
```

The application layer owns deduplication/current-state semantics. An older sequence cannot replace a newer durable state.

## 5. Context loss / completion

### `HandleDraftContextTransition`

Input:

```text
- ContextKey
- ContextGeneration
- TransitionReason
- CompletionEvidence
- ObservedAtUtc
```

Outcomes:

```text
DraftKeptRecoverable
DraftRemovedAfterStrongCompletion
DraftRemovedAfterStableClear
CheckpointRequested
NoActiveDraft
StaleIgnored
```

Focus loss alone cannot select `DraftRemovedAfterStrongCompletion`.

## 6. Recovery list

### `ListRecoverableDrafts`

Input: current time only.

Output:

```text
RecoverableDraftSummaryDto[]
- DraftId
- ApplicationDisplayName
- PresentationKind            // coarse enum; never raw site/window/field label
- UpdatedAtUtc
- ExpiresAtUtc
- RestoreAvailability
- PreviewAvailability
- CopyAvailability
```

Important: the query does **not** decrypt draft bodies. No preview snippet is generated automatically for the list in MVP.

## 7. Preview

### `PreviewDraft`

Input: `DraftId`.

Output:

```text
PreviewDraftResult
- Available(DraftPreviewDto)
- NotFound
- Expired
- ProtectionUnavailable
- CorruptRecord
- UnsupportedProtectionVersion
```

`DraftPreviewDto` contains transient plaintext for the explicit Preview surface only. The UI must dispose/release references when the preview closes; no cache of all previews is maintained.

Preview never mutates the destination application.

## 8. Copy

### `CopyDraft`

Input: `DraftId`.

Outcome:

```text
Copied
NotFound
Expired
ProtectionUnavailable
ClipboardUnavailable
```

The command decrypts only the selected draft, performs one explicit clipboard write, then drops its plaintext reference. Copy does not delete the recoverable draft.

## 9. Restore

### `RestoreDraft`

Input:

```text
RestoreDraftCommand
- DraftId
```

No caller-supplied target handle, confidence, `force`, or `skipSecurityCheck` parameter exists.

The use case performs:

1. load protected record;
2. reject expiry;
3. locate a fresh live target;
4. resolve compatible profile;
5. fresh secure/private classification;
6. deterministic recovery-evidence evaluation;
7. target stability check;
8. decrypt selected draft only after mutation is authorized;
9. supported adapter write;
10. safe post-write verification when capability exists;
11. delete recoverable record only after verified success.

Outcome:

```text
RestoreDraftResult
- VerifiedRestored
- AppliedButUnverified
- DraftNotFound
- DraftExpired
- TargetUnavailable
- TargetAmbiguous
- TargetMismatch
- TargetUnsafe
- TargetChanged
- WriteUnsupported
- WriteFailed
- VerificationMismatch
- ProtectionUnavailable
```

`AppliedButUnverified`, `WriteFailed`, and `VerificationMismatch` keep the recoverable record.

## 10. Discard

### `DiscardDraft`

Input: `DraftId`.

Outcome:

```text
Discarded
AlreadyAbsent
PersistenceFailed
```

Idempotent from the user's perspective.

### `DiscardAllDrafts`

Outcome:

```text
AllDiscarded(count)
AlreadyEmpty
PersistenceFailed
```

Must not decrypt payloads.

## 11. Settings

### `GetSettings`

Returns validated effective settings only.

### `UpdateSettings`

Input is a typed settings DTO, not arbitrary key/value pairs.

Outcome:

```text
Updated
RejectedInvalidValue
PersistenceFailed
```

Retention changes take effect prospectively according to `RETENTION_POLICY_SPEC.md`; they do not revive expired records.

## 12. Cancellation

Cancellation is cooperative. A canceled metadata/read/storage operation returns/propagates cancellation without changing a deny into allow. A canceled Restore after mutation begins follows the adapter verification contract and must not falsely report `VerifiedRestored`.

## 13. UI ownership boundary

ViewModels may translate typed results into display states/copy. They may not:

- recalculate security eligibility;
- reinterpret `Ambiguous` as `StrongMatch`;
- decrypt records directly;
- invent fallback Restore mechanisms;
- inspect SQLite/DPAPI/UIA objects;
- expose raw internal error details to the user.
