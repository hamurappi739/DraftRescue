# Future Contract Catalog

**Purpose:** give future implementation agents concrete interfaces to implement without freezing Phase 0 to unvalidated Windows APIs. `USE_CASE_CONTRACTS_V1.md`, `TARGET_ADAPTER_CONTRACTS.md`, and `REPOSITORY_TRANSACTION_SEMANTICS.md` are the more precise behavioral companions.

Names below are conceptual. Exact signatures may change through focused ADRs, but responsibilities and data boundaries should remain.

## 1. Metadata observation boundary

```csharp
public interface IForegroundContextSource
{
    IAsyncEnumerable<ForegroundContextChanged> WatchAsync(CancellationToken cancellationToken);
}
```

Event must contain **metadata only**, not field text.

## 2. Candidate field locator

```csharp
public interface ICandidateFieldLocator
{
    Task<CandidateFieldMetadata?> LocateFocusedEditableFieldAsync(
        ForegroundContext context,
        CancellationToken cancellationToken);
}
```

Metadata may include normalized control role, editability, read-only flag, protected-content signal, safe stable IDs/capabilities, and opaque platform token valid only for the short-lived operation.

## 3. Secure guard

Existing `ISecureInputGuard` remains the policy gate.

Desired conceptual call:

```csharp
Task<CaptureEligibility> EvaluateAsync(
    CandidateFieldMetadata field,
    CancellationToken cancellationToken);
```

No raw text parameter.

## 4. Text snapshot reader

```csharp
public interface IEligibleFieldTextReader
{
    Task<FieldTextSnapshot> ReadAsync(
        AllowedFieldHandle allowedField,
        CancellationToken cancellationToken);
}
```

The type name intentionally communicates that ordinary arbitrary fields are not accepted. The object is created only after the guard succeeds. Per ADR 0039/0040 it is single-consumption, generation/binding-bound, short-lived, and non-serializable; a fresh capability is required for every read attempt.

## 5. Draft tracker

```csharp
public interface IDraftTracker
{
    Task ApplySnapshotAsync(AllowedDraftSnapshot snapshot, CancellationToken ct);
    Task HandleContextLossAsync(ContextKey key, ContextLossReason reason, CancellationToken ct);
    Task HandleCompletionAsync(ContextKey key, CompletionEvidence evidence, CancellationToken ct);
}
```

Tracker owns current state transitions, not persistence implementation details.

## 6. Protected payload boundary

Prefer explicit protection abstraction:

```csharp
public interface IDraftProtector
{
    ProtectedDraftPayload Protect(DraftPlaintext plaintext, DraftProtectionContext context);
    DraftPlaintext Unprotect(ProtectedDraftPayload payload, DraftProtectionContext context);
}
```

Repository should not offer arbitrary `SaveStringAsync(string text)` APIs.

## 7. Repository

```csharp
public interface IDraftRepository
{
    Task<ProtectedDraftUpsertResult> UpsertAsync(ProtectedDraftRecord record, CancellationToken ct);
    Task<ProtectedDraftRecord?> GetAsync(DraftId id, CancellationToken ct);
    Task<IReadOnlyList<ProtectedDraftMetadata>> ListRecoverableAsync(DateTimeOffset now, CancellationToken ct);
    Task DeleteAsync(DraftId id, CancellationToken ct);
    Task DeleteExpiredAsync(DateTimeOffset now, CancellationToken ct);
}
```

Listing should avoid decrypting every draft just to render metadata if the UI can defer preview decryption.

## 8. Completion detector

```csharp
public interface ICompletionDetector
{
    Task<CompletionAssessment> AssessAsync(
        ActiveDraftContext context,
        AppProfile profile,
        CancellationToken ct);
}
```

Unknown does not mean delete.

## 9. Recovery matcher

```csharp
public interface IRecoveryMatcher
{
    Task<RecoveryMatchResult> MatchAsync(
        RecoverableDraftMetadata draft,
        LiveCandidateTarget target,
        CancellationToken ct);
}
```

Result is categorical/profile-aware, not a public confidence percentage.

## 10. Restore service

```csharp
public interface IRestoreService
{
    Task<RestoreResult> RestoreAsync(DraftId draftId, CancellationToken ct);
}
```

The service itself performs fresh lookup/security/match checks. UI must not pass a boolean such as `force=true`.

## 11. Retention

```csharp
public interface IRetentionPolicy
{
    DateTimeOffset CalculateExpiry(DateTimeOffset updatedAt, RetentionOptions options);
    bool IsExpired(DateTimeOffset now, DateTimeOffset expiresAt);
}
```

## 12. App profile resolver

```csharp
public interface IAppProfileResolver
{
    AppProfileMatch Resolve(ForegroundApplicationIdentity app);
}
```

Unsupported versions/unknown app identity return explicit unsupported state.

Repository upsert must reject stale `SnapshotSequence` inside the transaction.

## 13. Presentation query boundary

Desktop should consume purpose-built application queries/commands, for example:

```text
ListRecoverableDrafts
PreviewDraft
CopyDraft
RestoreDraft
DiscardDraft
DiscardAllDrafts
GetSettings
UpdateRetention
```

Desktop must not query repositories or decryptors directly. Recovery-list DTOs are metadata-only and must not require payload decryption. See `USE_CASE_CONTRACTS_V1.md`.

## 14. Cancellation rule

Every platform/storage operation that can block must accept cancellation/timeouts. A canceled or timed-out security query never turns into permission.
