# Target Adapter Contracts

**Status:** accepted boundary for Phase 1/6/7/8 adapters.

## 1. Why adapters exist

Windows/native/Chromium/Electron controls do not expose identical read/write/completion capabilities. Core application logic must consume explicit capabilities rather than probe arbitrary APIs until something works.

## 2. Capability descriptor

Conceptual:

```text
TargetCapabilities
- CanInspectMetadata
- CanReadSnapshot
- CanObserveChangeSignal
- CanDetectStrongCompletion
- CanProduceStableFieldIdentity
- CanProduceWindowContextFingerprint
- CanProduceBrowserOriginFingerprint
- CanDirectRestore
- CanVerifyRestore
```

Capabilities are profile/certification outcomes, not promises inferred from process name alone.

## 3. Metadata adapter

```csharp
public interface ITargetMetadataAdapter
{
    Task<CandidateFieldMetadata?> LocateFocusedCandidateAsync(
        ForegroundContext context,
        CancellationToken ct);
}
```

Must not read draft body.

## 4. Eligible reader

```csharp
public interface IEligibleFieldTextReader
{
    Task<EligibleReadResult> ReadAsync(
        AllowedFieldHandle handle,
        SnapshotSequence sequence,
        CancellationToken ct);
}
```

No method accepts a generic AutomationElement/control pointer from arbitrary callers.

## 5. Change signal

A target adapter may provide a metadata/content-changed **signal** that tells orchestration a fresh snapshot may be useful. The signal itself does not contain the user's typed characters.

No generic keyboard-event stream is introduced to compensate for a missing change signal.

## 6. Restore target locator

```csharp
public interface IRestoreTargetLocator
{
    Task<LiveTargetCandidates> LocateAsync(
        RecoverableDraftMetadata draft,
        CancellationToken ct);
}
```

Candidates contain metadata/evidence only. The draft body is not required to locate a target.

## 7. Restore adapter

```csharp
public interface IRestoreAdapter
{
    Task<RestoreWriteResult> WriteAsync(
        ValidatedRestoreTarget target,
        RestorePlaintext plaintext,
        CancellationToken ct);

    Task<RestoreVerificationResult> VerifyAsync(
        ValidatedRestoreTarget target,
        RestoreVerificationToken token,
        CancellationToken ct);
}
```

`ValidatedRestoreTarget` is minted only after fresh security + StrongMatch + stability checks. A generic public `SetText(target, string)` API is forbidden.

## 8. No SendKeys fallback

If a target has no certified write capability, it is Copy-only. The adapter layer does not silently fall back to global synthetic keyboard input or paste automation.

## 9. Adapter timeout isolation

All provider calls run under the platform threading/timeout policy. Timeout becomes typed failure/Unknown, not a permission fallback.

## 10. Adapter certification

Every adapter mechanism is tied to profile/version certification tests for:

- ordinary fields;
- protected/credential/payment negative cases;
- stale element/recreated tree;
- change-signal storms;
- direct restore wrong-target prevention;
- verification behavior;
- timeout/resource bounds.
