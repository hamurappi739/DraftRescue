# Implementation Type Blueprint

**Purpose:** give future Cursor tasks exact type intent without implementing Phase 1+ now.

Names are canonical enough to start from; signature adjustments require staying within the same boundary.

## Domain

```text
DraftId(Guid Value)
ApplicationId(string Value)
AppProfileId(string Value)
ContextGeneration(long Value)
SnapshotSequence(long Value)
FingerprintToken(byte[16] Value, FingerprintKind Kind, int Version)
ContextFingerprintSet(...)
DraftLifecycleState
DraftPresentationKind
RecoverableState
CompletionAssessment
RecoveryMatchKind
```

## Application security

See `PHASE2_TYPE_BLUEPRINT.md` for the refined Phase 2 model.

```text
CandidateFieldMetadata
CaptureEligibility
CaptureDenialReason
AllowedFieldHandle
ISecureInputGuard
```

`AllowedFieldHandle` constructor should be internal to the security/orchestration assembly path or created through a factory so random callers cannot forge permission.

## Observation

```text
ForegroundApplicationIdentity
ForegroundContext
ForegroundContextChanged
IForegroundContextSource
ICandidateFieldLocator
```

No text in these types.

## Text read

```text
FieldTextSnapshot
IEligibleFieldTextReader
```

Reader accepts only `AllowedFieldHandle`.

## Tracking

```text
AllowedDraftSnapshot
ActiveDraft
IDraftTracker
ICompletionDetector
IPersistenceCheckpointScheduler
```

## Protection/persistence

```text
DraftPlaintext
ProtectedDraftPayload
DraftProtectionContext
IDraftProtector
ProtectedDraftRecord
ProtectedDraftMetadata
ProtectedDraftUpsertResult
IDraftRepository
```

Repository never accepts `DraftPlaintext`.

## Profiles

```text
AppProfile
AppProfileResolution
TargetCompatibility
IAppProfileResolver
IAppProfileManifestLoader
```

## Recovery

```text
LiveCandidateTarget
RecoveryMatchEvidence
RecoveryMatchResult
RecoveryEvidenceSet
ValidatedRestoreTarget
IRecoveryMatcher
IRestoreTargetLocator
IRestoreAdapter
IRestoreService
```

## UI application commands/queries

```text
ListRecoverableDraftsQuery
PreviewDraftQuery
CopyDraftCommand
RestoreDraftCommand
DiscardDraftCommand
DiscardAllDraftsCommand
GetSettingsQuery
UpdateSettingsCommand
RecoverableDraftSummaryDto
DraftPreviewDto
RestoreDraftResult
```

UI gets purpose-built result models, not infrastructure/domain internals.

## Infrastructure/platform helpers

```text
IClock
IInstallationSecretStore
IFingerprintService
IClipboardService
IAppDataPathProvider
IDraftRescueErrorMapper
```

Clipboard exists only for explicit Copy path.

## Forbidden convenience types/APIs

Do not introduce:

```text
GlobalKeyLogger
KeyboardHistory
InputHistoryRepository
SaveTypedText(string)
LogCapturedText(...)
ForceRestore(...)
GetAllDecryptedDrafts()
CaptureAnyFocusedText()
```

If an implementation appears to need one, stop and revisit architecture.
