using System.Windows.Automation;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Models;
using DraftRescue.Application.Security;

namespace DraftRescue.Platform.Windows.Reading;

/// <summary>
/// One-shot ValuePattern reader for an explicitly certified bounded
/// single-line surface. ValuePattern has no provider-side maximum-length
/// parameter, so the complete provider result is rejected when it exceeds the
/// configured post-read limit; it is never truncated.
/// </summary>
public sealed class ValuePatternBoundedReader : IEligibleFieldTextReader
{
    private readonly IReadCapabilityClaimer _claimer;
    private readonly ICertifiedValueReadTargetResolver _targets;
    private readonly IValuePatternAccessor _accessor;
    private readonly IMonotonicClock _clock;

    public ValuePatternBoundedReader(
        IReadCapabilityClaimer claimer,
        ICertifiedValueReadTargetResolver targets,
        IValuePatternAccessor accessor,
        IMonotonicClock clock)
    {
        _claimer = claimer ?? throw new ArgumentNullException(nameof(claimer));
        _targets = targets ?? throw new ArgumentNullException(nameof(targets));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<EligibleTextReadResult> ReadOnceAsync(
        AllowedFieldHandle handle,
        ReadBudget budget,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(budget);

        var claim = _claimer.TryClaim(handle);
        if (claim != CapabilityClaimResult.Claimed)
        {
            return claim switch
            {
                CapabilityClaimResult.Expired => new EligibleTextReadResult.Expired(),
                CapabilityClaimResult.AlreadyConsumed => new EligibleTextReadResult.AlreadyConsumed(),
                CapabilityClaimResult.Revoked or CapabilityClaimResult.StaleContext or CapabilityClaimResult.BindingUnavailable => new EligibleTextReadResult.RevokedOrStale(),
                _ => new EligibleTextReadResult.RevokedOrStale()
            };
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new EligibleTextReadResult.Cancelled();
        }

        if (budget.Strategy != ReadStrategy.ValuePatternCertified ||
            !_targets.TryResolve(handle, out var target) ||
            !target.IsValuePatternCertified)
        {
            return budget.Strategy == ReadStrategy.ValuePatternCertified
                ? new EligibleTextReadResult.TargetChanged()
                : new EligibleTextReadResult.UnsupportedReadStrategy();
        }

        Task<string> readTask;
        try
        {
            readTask = _accessor.ReadValueAsync(target.Element, cancellationToken);
        }
        catch (ValuePatternUnavailableException)
        {
            return new EligibleTextReadResult.UnsupportedReadStrategy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EligibleTextReadResult.Cancelled();
        }
        catch (Exception)
        {
            return new EligibleTextReadResult.ProviderFailure("DR-UIA-3002");
        }
        _ = ObserveLateFailureAsync(readTask);

        string text;
        try
        {
            text = await readTask.WaitAsync(budget.ProviderTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EligibleTextReadResult.Cancelled();
        }
        catch (TimeoutException)
        {
            return new EligibleTextReadResult.Timeout();
        }
        catch (ValuePatternUnavailableException)
        {
            return new EligibleTextReadResult.UnsupportedReadStrategy();
        }
        catch (OperationCanceledException)
        {
            return new EligibleTextReadResult.ProviderFailure("DR-UIA-3002");
        }
        catch (Exception)
        {
            return new EligibleTextReadResult.ProviderFailure("DR-UIA-3002");
        }

        if (!_targets.IsCurrent(target))
        {
            return new EligibleTextReadResult.TargetChanged();
        }

        if (text.Length > budget.MaximumUtf16CodeUnits)
        {
            return new EligibleTextReadResult.TooLarge(
                text.Length,
                budget.MaximumUtf16CodeUnits);
        }

        var snapshot = FieldTextSnapshot.Create(
            target.ContextGeneration,
            target.SnapshotSequence,
            Guid.NewGuid(),
            target.ProfileId,
            ReadStrategy.ValuePatternCertified,
            _clock.GetTimestampMilliseconds(),
            DateTimeOffset.UtcNow,
            text);
        return snapshot.IsEmpty
            ? new EligibleTextReadResult.Empty(snapshot)
            : new EligibleTextReadResult.Success(snapshot);
    }

    private static async Task ObserveLateFailureAsync(Task<string> readTask)
    {
        try
        {
            await readTask.ConfigureAwait(false);
        }
        catch
        {
            // Late provider completion is discarded without logging or retry.
        }
    }
}

public sealed record CertifiedValueReadTarget(
    AutomationElement Element,
    ulong ContextGeneration,
    ulong SnapshotSequence,
    string ProfileId,
    bool IsValuePatternCertified);

public interface ICertifiedValueReadTargetResolver
{
    bool TryResolve(AllowedFieldHandle handle, out CertifiedValueReadTarget target);
    bool IsCurrent(CertifiedValueReadTarget target);
}

public interface IValuePatternAccessor
{
    Task<string> ReadValueAsync(
        AutomationElement element,
        CancellationToken cancellationToken);
}

public sealed class ValuePatternUnavailableException : Exception
{
}

/// <summary>
/// Windows UI Automation adapter for certified single-line/value surfaces.
/// No alternate pattern, keyboard or clipboard path is attempted.
/// </summary>
public sealed class UiaValuePatternAccessor : IValuePatternAccessor
{
    public Task<string> ReadValueAsync(
        AutomationElement element,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(element);

        return Task.Run(() =>
        {
            if (!element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
            {
                throw new ValuePatternUnavailableException();
            }

            var valuePattern = (ValuePattern)pattern;
            return valuePattern.Current.Value;
        }, cancellationToken);
    }
}
