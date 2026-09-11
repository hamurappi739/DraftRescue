namespace DraftRescue.Application.Security;

public enum SecuritySignalState
{
    Known = 0,
    Unknown = 1,
    Unavailable = 2,
    Failed = 3
}

/// <summary>
/// Typed metadata signal. A non-known state never silently becomes a safe value.
/// </summary>
public readonly record struct SecuritySignal<T>(SecuritySignalState State, T Value, string? FailureCode)
{
    public bool IsKnown => State == SecuritySignalState.Known;

    public static SecuritySignal<T> Known(T value) => new(SecuritySignalState.Known, value, null);

    public static SecuritySignal<T> Unknown() => new(SecuritySignalState.Unknown, default!, null);

    public static SecuritySignal<T> Unavailable() => new(SecuritySignalState.Unavailable, default!, null);

    public static SecuritySignal<T> Failed(string stableCode) => new(SecuritySignalState.Failed, default!, stableCode);
}

public enum ProfileResolutionStatus
{
    Resolved = 0,
    Missing = 1,
    Invalid = 2,
    Ambiguous = 3,
    IncompatibleVersion = 4
}

public enum TargetVersionStatus
{
    Certified = 0,
    Unknown = 1,
    Incompatible = 2,
    NotApplicable = 3
}

public enum IntegrityBoundaryStatus
{
    Accessible = 0,
    Inaccessible = 1,
    Unknown = 2
}

public enum PrivateModeStatus
{
    NotApplicable = 0,
    ConfirmedNormal = 1,
    ConfirmedPrivate = 2,
    Unknown = 3
}

public enum ProviderHealthStatus
{
    Healthy = 0,
    Stale = 1,
    Timeout = 2,
    AccessDenied = 3,
    Failed = 4
}

public enum SensitivePurpose
{
    None = 0,
    Credential = 1,
    Password = 2,
    PinOrOtp = 3,
    Payment = 4,
    Banking = 5,
    Unknown = 6
}

public enum PositiveAllowPredicateStatus
{
    Satisfied = 0,
    NotSatisfied = 1,
    Indeterminate = 2
}

public enum BindingStatus
{
    Current = 0,
    Stale = 1,
    Missing = 2
}

/// <summary>
/// Complete, metadata-only security evidence for one focused candidate.
/// </summary>
public sealed record SecurityEvidenceSet(
    Guid CandidateId,
    Guid BindingId,
    ulong ContextGeneration,
    ulong ObservationSequence,
    ProfileResolutionStatus ProfileResolution,
    string? ProfileId,
    int ProfileRevision,
    TargetVersionStatus TargetVersionStatus,
    IntegrityBoundaryStatus IntegrityBoundary,
    PrivateModeStatus PrivateMode,
    SecuritySignal<bool> ProtectedContent,
    SecuritySignal<bool> Editable,
    SecuritySignal<bool> ReadOnly,
    ProviderHealthStatus ProviderHealth,
    SensitivePurpose SensitivePurpose,
    PositiveAllowPredicateStatus PositiveAllowPredicate,
    BindingStatus BindingStatus,
    DraftRescue.Application.Models.UiaControlKind ControlKind,
    DraftRescue.Application.Models.UiaFrameworkKind FrameworkKind);

public abstract record SecurityDecision
{
    private SecurityDecision() { }

    public sealed record Allowed(
        Guid CandidateId,
        Guid BindingId,
        ulong ContextGeneration,
        string ProfileId,
        int ProfileRevision) : SecurityDecision;

    public sealed record Denied(CaptureDenialReason Reason, string StableErrorCode) : SecurityDecision;
}
