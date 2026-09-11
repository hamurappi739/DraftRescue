namespace DraftRescue.Application.Security;

public interface IMonotonicClock
{
    long GetTimestampMilliseconds();
}

public sealed class StopwatchMonotonicClock : IMonotonicClock
{
    public long GetTimestampMilliseconds() =>
        (long)(System.Diagnostics.Stopwatch.GetTimestamp() * 1000d / System.Diagnostics.Stopwatch.Frequency);
}

public sealed record CapabilityBindingSnapshot(
    Guid CandidateId,
    Guid BindingId,
    ulong ContextGeneration,
    string ProfileId,
    int ProfileRevision,
    bool IsCurrent);

public interface ICapabilityBindingRegistry
{
    bool TryGetCurrent(Guid bindingId, out CapabilityBindingSnapshot binding);
}

/// <summary>
/// Opaque process-local capability. The constructor and state are intentionally not public.
/// </summary>
public sealed class AllowedFieldHandle
{
    private int _state;

    internal AllowedFieldHandle(
        Guid capabilityId,
        Guid candidateId,
        Guid bindingId,
        ulong contextGeneration,
        string profileId,
        int profileRevision,
        long issuedAtMilliseconds,
        long deadlineMilliseconds)
    {
        CapabilityId = capabilityId;
        CandidateId = candidateId;
        BindingId = bindingId;
        ContextGeneration = contextGeneration;
        ProfileId = profileId;
        ProfileRevision = profileRevision;
        IssuedAtMilliseconds = issuedAtMilliseconds;
        DeadlineMilliseconds = deadlineMilliseconds;
    }

    internal Guid CapabilityId { get; }
    internal Guid CandidateId { get; }
    internal Guid BindingId { get; }
    internal ulong ContextGeneration { get; }
    internal string ProfileId { get; }
    internal int ProfileRevision { get; }
    internal long IssuedAtMilliseconds { get; }
    internal long DeadlineMilliseconds { get; }
    internal int State => Volatile.Read(ref _state);

    internal bool TryTransition(int expected, int next) =>
        Interlocked.CompareExchange(ref _state, next, expected) == expected;

    public override string ToString() => nameof(AllowedFieldHandle);
}

public abstract record CapabilityIssueResult
{
    private CapabilityIssueResult() { }

    public sealed record Issued(AllowedFieldHandle Handle) : CapabilityIssueResult;

    public sealed record Denied(CaptureDenialReason Reason, string StableErrorCode) : CapabilityIssueResult;
}

public enum CapabilityClaimResult
{
    Claimed = 0,
    AlreadyConsumed = 1,
    Revoked = 2,
    Expired = 3,
    StaleContext = 4,
    BindingUnavailable = 5,
    Invalid = 6
}

/// <summary>
/// Mints a one-read capability only after a current, complete Allowed decision.
/// </summary>
public sealed class AllowedFieldHandleIssuer
{
    public const long DefaultMaximumAgeMilliseconds = 1000;
    public const long HardMaximumAgeMilliseconds = 2000;

    private readonly ICapabilityBindingRegistry _bindings;
    private readonly IMonotonicClock _clock;
    private readonly long _maximumAgeMilliseconds;

    public AllowedFieldHandleIssuer(
        ICapabilityBindingRegistry bindings,
        IMonotonicClock clock,
        long maximumAgeMilliseconds = DefaultMaximumAgeMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(clock);
        if (maximumAgeMilliseconds <= 0 || maximumAgeMilliseconds > HardMaximumAgeMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAgeMilliseconds));
        }

        _bindings = bindings;
        _clock = clock;
        _maximumAgeMilliseconds = maximumAgeMilliseconds;
    }

    public CapabilityIssueResult Issue(SecurityEvidenceSet evidence, SecurityDecision decision)
    {
        if (decision is not SecurityDecision.Allowed allowed ||
            allowed.CandidateId != evidence.CandidateId ||
            allowed.BindingId != evidence.BindingId ||
            allowed.ContextGeneration != evidence.ContextGeneration ||
            !string.Equals(allowed.ProfileId, evidence.ProfileId, StringComparison.Ordinal) ||
            allowed.ProfileRevision != evidence.ProfileRevision)
        {
            return new CapabilityIssueResult.Denied(CaptureDenialReason.UncertainSecureState, "DR-SEC-2005");
        }

        if (!_bindings.TryGetCurrent(evidence.BindingId, out var binding) ||
            !binding.IsCurrent ||
            binding.CandidateId != evidence.CandidateId ||
            binding.ContextGeneration != evidence.ContextGeneration ||
            !string.Equals(binding.ProfileId, evidence.ProfileId, StringComparison.Ordinal) ||
            binding.ProfileRevision != evidence.ProfileRevision)
        {
            return new CapabilityIssueResult.Denied(CaptureDenialReason.StaleContext, "DR-OBS-1003");
        }

        var issuedAt = _clock.GetTimestampMilliseconds();
        var deadline = checked(issuedAt + _maximumAgeMilliseconds);
        var handle = new AllowedFieldHandle(
            Guid.NewGuid(), evidence.CandidateId, evidence.BindingId, evidence.ContextGeneration,
            evidence.ProfileId!, evidence.ProfileRevision, issuedAt, deadline);
        return new CapabilityIssueResult.Issued(handle);
    }
}

/// <summary>
/// Validates and atomically consumes a capability before any future content read.
/// </summary>
public sealed class AllowedFieldHandleValidator
{
    private readonly ICapabilityBindingRegistry _bindings;
    private readonly IMonotonicClock _clock;

    public AllowedFieldHandleValidator(ICapabilityBindingRegistry bindings, IMonotonicClock clock)
    {
        _bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public CapabilityClaimResult TryClaim(AllowedFieldHandle? handle, SecurityEvidenceSet currentEvidence)
    {
        if (handle is null)
        {
            return CapabilityClaimResult.Invalid;
        }

        var now = _clock.GetTimestampMilliseconds();
        if (handle.State != 0)
        {
            return handle.State == 1 ? CapabilityClaimResult.AlreadyConsumed : CapabilityClaimResult.Revoked;
        }

        if (now >= handle.DeadlineMilliseconds)
        {
            handle.TryTransition(0, 2);
            return CapabilityClaimResult.Expired;
        }

        if (!Matches(handle, currentEvidence))
        {
            handle.TryTransition(0, 2);
            return CapabilityClaimResult.StaleContext;
        }

        if (!_bindings.TryGetCurrent(handle.BindingId, out var binding) || !binding.IsCurrent)
        {
            handle.TryTransition(0, 2);
            return CapabilityClaimResult.BindingUnavailable;
        }

        if (!handle.TryTransition(0, 1))
        {
            return handle.State == 1 ? CapabilityClaimResult.AlreadyConsumed : CapabilityClaimResult.Revoked;
        }

        return CapabilityClaimResult.Claimed;
    }

    public bool Revoke(AllowedFieldHandle? handle) => handle is not null && handle.TryTransition(0, 2);

    private static bool Matches(AllowedFieldHandle handle, SecurityEvidenceSet evidence) =>
        handle.CandidateId == evidence.CandidateId &&
        handle.BindingId == evidence.BindingId &&
        handle.ContextGeneration == evidence.ContextGeneration &&
        handle.ProfileRevision == evidence.ProfileRevision &&
        string.Equals(handle.ProfileId, evidence.ProfileId, StringComparison.Ordinal) &&
        evidence.BindingStatus == BindingStatus.Current;
}
