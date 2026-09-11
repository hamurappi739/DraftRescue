# Phase 2 Type Blueprint

These are implementation-intent types for future coding work. Signatures may be adjusted only without weakening boundaries.

## Application security types

```csharp
public enum SecuritySignalStatus { Known, Unknown, Unavailable, Failed }

public enum PrivateModeState { NotApplicable, ConfirmedNormal, ConfirmedPrivate, Unknown }
public enum ProviderHealth { Healthy, Stale, Timeout, AccessDenied, Failed }
public enum SensitivePurpose { None, Credential, Password, PinOrOtp, Payment, Banking, Unknown }
public enum PositiveAllowState { Satisfied, NotSatisfied, Indeterminate }

public sealed record SecurityEvidenceSet(/* structural fields only */);

public abstract record SecurityDecision
{
    public sealed record Allowed(AllowBasis Basis) : SecurityDecision;
    public sealed record Denied(CaptureDenialReason Reason, DraftRescueErrorCode Code) : SecurityDecision;
}
```

## Policy

```csharp
public interface ISecurityPolicyEvaluator
{
    SecurityDecision Evaluate(SecurityEvidenceSet evidence);
}
```

Pure function. No UIA, I/O, clock or logging dependencies required for truth evaluation.

## Signal collection

```csharp
public interface ISecurityEvidenceCollector
{
    ValueTask<SecurityEvidenceCollectionResult> CollectAsync(
        TargetFieldCandidateMetadata candidate,
        ResolvedAppProfile profile,
        CancellationToken ct);
}
```

Collector may call audited metadata-only platform adapters. It never calls content APIs.

## Capability issuer

```csharp
public interface IAllowedFieldCapabilityIssuer
{
    ValueTask<AllowedFieldHandleIssueResult> TryIssueAsync(
        SecurityDecision.Allowed decision,
        TargetFieldCandidateMetadata candidate,
        CancellationToken ct);
}
```

## Claim/validation boundary for future Phase 3

```csharp
public interface IAllowedFieldCapabilityValidator
{
    ValueTask<AllowedFieldClaimResult> TryClaimAsync(
        AllowedFieldHandle handle,
        CurrentContextStamp current,
        CancellationToken ct);
}
```

The successful claim produces a platform-private binding usable by the future reader; it does not expose `AutomationElement` to Application/Desktop.

## AllowedFieldHandle

Prefer sealed reference type with no public constructor and no serialization attributes. Its string representation is redacted.

## Existing Phase-0 placeholder

The current `CaptureEligibility` / `ISecureInputGuard` skeleton is a placeholder. Phase 2 may refine it toward the above model, but compatibility shims must not leave a second permissive path.
