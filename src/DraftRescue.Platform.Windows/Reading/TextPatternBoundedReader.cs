using System.Windows.Automation;
using DraftRescue.Application.Contracts.Reading;
using DraftRescue.Application.Models;
using DraftRescue.Application.Security;

namespace DraftRescue.Platform.Windows.Reading;

/// <summary>
/// One-shot bounded TextPattern reader. The capability is claimed before the
/// target is resolved for content and is spent on every attempt, including
/// timeout, provider failure, oversize and target-change outcomes.
/// </summary>
public sealed class TextPatternBoundedReader : IEligibleFieldTextReader
{
    private readonly IReadCapabilityClaimer _claimer;
    private readonly ICertifiedReadTargetResolver _targets;
    private readonly ITextPatternDocumentAccessor _accessor;
    private readonly IMonotonicClock _clock;

    public TextPatternBoundedReader(
        IReadCapabilityClaimer claimer,
        ICertifiedReadTargetResolver targets,
        ITextPatternDocumentAccessor accessor,
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

        if (budget.Strategy != ReadStrategy.TextPatternDocument ||
            !_targets.TryResolve(handle, out var target) ||
            !target.IsTextPatternCertified)
        {
            return budget.Strategy == ReadStrategy.TextPatternDocument
                ? new EligibleTextReadResult.TargetChanged()
                : new EligibleTextReadResult.UnsupportedReadStrategy();
        }

        Task<string> readTask;
        try
        {
            readTask = _accessor.ReadDocumentAsync(
                target.Element,
                checked(budget.MaximumUtf16CodeUnits + 1),
                cancellationToken);
        }
        catch (TextPatternUnavailableException)
        {
            return new EligibleTextReadResult.UnsupportedReadStrategy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EligibleTextReadResult.Cancelled();
        }
        catch (Exception)
        {
            // Provider exception details are deliberately not surfaced.
            return new EligibleTextReadResult.ProviderFailure("DR-UIA-3001");
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
        catch (TextPatternUnavailableException)
        {
            return new EligibleTextReadResult.UnsupportedReadStrategy();
        }
        catch (OperationCanceledException)
        {
            return new EligibleTextReadResult.ProviderFailure("DR-UIA-3001");
        }
        catch (Exception)
        {
            // Provider exception details are deliberately not surfaced.
            return new EligibleTextReadResult.ProviderFailure("DR-UIA-3001");
        }

        if (!_targets.IsCurrent(target))
        {
            return new EligibleTextReadResult.TargetChanged();
        }

        if (text.Length > budget.MaximumUtf16CodeUnits)
        {
            return new EligibleTextReadResult.TooLarge(
                budget.MaximumUtf16CodeUnits + 1,
                budget.MaximumUtf16CodeUnits);
        }

        var snapshot = FieldTextSnapshot.Create(
            target.ContextGeneration,
            target.SnapshotSequence,
            Guid.NewGuid(),
            target.ProfileId,
            ReadStrategy.TextPatternDocument,
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
