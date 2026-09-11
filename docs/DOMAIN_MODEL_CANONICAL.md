# Canonical Domain Model

**Status:** accepted Phase-0 design baseline. Implementation may change syntax, but not the privacy and ownership boundaries without an ADR.

## 1. Design rule

The domain model describes recoverable draft state, not typing events. There is no durable `Keystroke`, `TextEditEvent`, `Revision`, or `InputHistory` aggregate.

## 2. Core identifiers

```text
DraftId
- opaque random 128-bit identifier
- no user text or semantic information encoded

ApplicationId
- normalized stable application identity
- not sufficient by itself for recovery matching

AppProfileId
- versioned profile identifier

ContextFingerprintSet
- versioned set of keyed correlation tokens
- contains no raw title/URL/field label when a derived token is sufficient
```

## 3. Draft aggregate

Conceptual shape:

```text
DraftSession
- DraftId
- ApplicationId
- AppProfileId?
- ContextFingerprintSet
- CreatedAtUtc
- UpdatedAtUtc
- ExpiresAtUtc
- LifecycleState
- ProtectedPayloadReference
- ProtectionFormatVersion
- MatchMetadataVersion
- LastCompletionAssessment
```

`DraftSession` is the one logical current snapshot for a recoverable draft. Updating text replaces the current protected payload. It does not append a durable revision.

## 4. Plaintext-only transient types

The following types are process-memory only and must never be serializable by default:

```text
DraftPlaintext
FieldTextSnapshot
AllowedDraftSnapshot
RestorePlaintext
PreviewPlaintext
```

Rules:

- no public parameterless constructors for serializers unless explicitly required;
- no `ToString()` override that returns content;
- debugger display must not expose content;
- no equality/hash implementation over plaintext content that can leak through diagnostics;
- do not place these objects into exception `Data`, structured logs, telemetry, or crash breadcrumbs.

## 5. CandidateFieldMetadata

Metadata gathered before reading text:

```text
CandidateFieldMetadata
- ApplicationIdentity
- ProcessId
- WindowHandleToken
- ControlHandleToken?
- FrameworkHint
- ControlType
- IsEnabled
- IsReadOnly
- IsPasswordSignal
- AutomationIdToken?
- ClassNameToken?
- BoundingRectBucket?
- SupportedReadCapabilities
- SupportedWriteCapabilities
- BrowserPrivacyEvidence
- ObservationTimestampUtc
```

This type MUST NOT contain field text.

## 6. AllowedFieldHandle

An `AllowedFieldHandle` is an opaque, process-local **one-read security capability** created only after metadata-only security classification returns Allowed and issuance revalidates current generation/binding/profile evidence.

Conceptual internal state:

```text
AllowedFieldHandle
- opaque capability nonce
- opaque ephemeral platform binding id
- application/profile identity + profile revision
- context generation
- monotonic issued/deadline timestamps
- operation = ReadSnapshot
- state = Fresh | Consumed | Revoked
```

Rules:

- one atomic claim maximum;
- default max age 1000 ms; hard cap 2000 ms;
- generation/binding/profile change revokes;
- any attempted read consumes after successful claim even if provider read fails;
- no serialization/persistence/string reconstruction;
- never used for Restore.

The future text reader accepts this capability rather than arbitrary UI Automation elements. See `ALLOWED_FIELD_HANDLE_CAPABILITY_SPEC.md`.

## 7. Capture eligibility

Canonical categories:

```text
Allowed
DeniedSecureContent
DeniedCredentialLike
DeniedBankingLike
DeniedPrivateBrowsing
DeniedUnsupportedApplication
DeniedUnsupportedControl
DeniedReadOnly
DeniedPolicy
DeniedUncertain
```

Only `Allowed` may create `AllowedFieldHandle` and proceed to a text read.

## 8. Completion assessment

```text
CompletionAssessment
- Unknown
- StillDraft
- ClearedByUser
- SubmittedOrSavedStrongEvidence
- ContextDestroyedWithoutCompletion
```

`Unknown` never means delete.

## 9. Recovery match result

Do not expose an arbitrary numeric confidence to product/UI code.

```text
RecoveryMatchKind
- NoMatch
- Ambiguous
- StrongMatch

RecoveryMatchEvidence
- app identity match
- profile match
- window/context tokens
- field tokens
- origin/site token if safely available
- geometry bucket if applicable
- freshness/generation evidence
- negative/conflict evidence
```

Only `StrongMatch` may enable direct Restore.

## 10. Restore state

```text
Recoverable
RestoreInProgress (transient only)
Restored
Copied
Discarded
Expired
Invalidated
```

`Copied` does not automatically delete the draft unless product policy later explicitly changes. `Restored`, `Discarded`, and `Expired` remove the recoverable record according to persistence semantics.

## 11. Time semantics

- use `DateTimeOffset` in application/domain contracts;
- persist normalized UTC timestamps;
- UI localizes only for presentation;
- expiry comparisons use an injectable clock;
- wall-clock jumps must not cause negative durations or revive expired records.

## 12. Invariants

1. No recoverable draft exists without a prior explicit `Allowed` classification.
2. No persistent record contains plaintext draft text.
3. One logical draft has at most one current durable payload.
4. An expired draft is never returned as recoverable.
5. A denied/uncertain field cannot be converted into allowed by timeout/error fallback.
6. Restore re-runs security and target matching; old eligibility is insufficient.
7. UI cannot manufacture a stronger recovery match than the application service produced.
