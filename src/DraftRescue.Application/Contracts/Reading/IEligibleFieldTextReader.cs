using DraftRescue.Application.Security;
using DraftRescue.Application.Models;

namespace DraftRescue.Application.Contracts.Reading;

/// <summary>
/// One-shot, capability-gated target text read. Implementations must claim the
/// handle before provider access and must never re-issue permission internally.
/// </summary>
public interface IEligibleFieldTextReader
{
    Task<EligibleTextReadResult> ReadOnceAsync(
        AllowedFieldHandle handle,
        ReadBudget budget,
        CancellationToken cancellationToken);
}

/// <summary>
/// Application-owned gate used by a reader to atomically spend a capability
/// before any provider content access. Implementations supply current evidence
/// from the active context; readers never mint or retry permission.
/// </summary>
public interface IReadCapabilityClaimer
{
    CapabilityClaimResult TryClaim(AllowedFieldHandle handle);
}

public enum ReadStrategy
{
    TextPatternDocument = 0,
    ValuePatternCertified = 1
}

public sealed record ReadBudget
{
    public const int DefaultMaximumUtf16CodeUnits = 65_536;
    public const int AbsoluteMaximumUtf16CodeUnits = 262_144;

    public ReadBudget(
        ReadStrategy strategy,
        int maximumUtf16CodeUnits = DefaultMaximumUtf16CodeUnits,
        TimeSpan? providerTimeout = null)
    {
        if (maximumUtf16CodeUnits <= 0 || maximumUtf16CodeUnits > AbsoluteMaximumUtf16CodeUnits)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumUtf16CodeUnits));
        }

        if (providerTimeout is { } timeout &&
            (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(30)))
        {
            throw new ArgumentOutOfRangeException(nameof(providerTimeout));
        }

        Strategy = strategy;
        MaximumUtf16CodeUnits = maximumUtf16CodeUnits;
        ProviderTimeout = providerTimeout ?? TimeSpan.FromSeconds(2);
    }

    public ReadStrategy Strategy { get; }
    public int MaximumUtf16CodeUnits { get; }
    public TimeSpan ProviderTimeout { get; }
}

public abstract record EligibleTextReadResult
{
    private EligibleTextReadResult() { }

    public sealed record Success(FieldTextSnapshot Snapshot) : EligibleTextReadResult;
    public sealed record Empty(FieldTextSnapshot Snapshot) : EligibleTextReadResult;
    public sealed record TooLarge(int ObservedAtLeast, int ConfiguredLimit) : EligibleTextReadResult;
    public sealed record Expired : EligibleTextReadResult;
    public sealed record AlreadyConsumed : EligibleTextReadResult;
    public sealed record RevokedOrStale : EligibleTextReadResult;
    public sealed record TargetChanged : EligibleTextReadResult;
    public sealed record UnsupportedReadStrategy : EligibleTextReadResult;
    public sealed record Timeout : EligibleTextReadResult;
    public sealed record ProviderFailure(string StableErrorCode) : EligibleTextReadResult;
    public sealed record Cancelled : EligibleTextReadResult;
}
