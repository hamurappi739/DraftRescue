using System.Collections.Concurrent;
using DraftRescue.Application.Contracts.Drafts;
using DraftRescue.Application.Models;
using DraftRescue.Application.Security;
using DraftRescue.Domain.Drafts;

namespace DraftRescue.Application.Drafts;

/// <summary>
/// Phase-3 current-state tracker. It accepts only complete snapshots and keeps
/// one record per logical field; failed reads never enter this type.
/// </summary>
public sealed class InMemoryDraftTracker : IDraftTracker
{
    public const long EmptyConfirmationDelayMilliseconds = 300;

    private readonly ConcurrentDictionary<DraftContextKey, DraftRecord> _records = new();
    private readonly IMonotonicClock _clock;

    public InMemoryDraftTracker(IMonotonicClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public DraftApplyResult ApplySnapshot(
        DraftContextKey context,
        FieldTextSnapshot snapshot,
        ulong currentContextGeneration)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.ContextGeneration != currentContextGeneration)
        {
            return DraftApplyResult.StaleIgnored;
        }

        while (true)
        {
            _records.TryGetValue(context, out var current);
            if (current is not null && snapshot.SnapshotSequence < current.LatestAcceptedSnapshotSequence)
            {
                return DraftApplyResult.StaleIgnored;
            }

            var next = BuildNextRecord(context, current, snapshot);
            if (current is null)
            {
                if (_records.TryAdd(context, next.Record))
                {
                    return next.Result;
                }

                continue;
            }

            if (_records.TryUpdate(context, next.Record, current))
            {
                return next.Result;
            }
        }
    }

    public DraftRecord? GetCurrent(DraftContextKey context) =>
        _records.TryGetValue(context, out var record) ? record : null;

    private (DraftRecord Record, DraftApplyResult Result) BuildNextRecord(
        DraftContextKey context,
        DraftRecord? current,
        FieldTextSnapshot snapshot)
    {
        var nowUtc = snapshot.CapturedAtUtc;
        var nowMonotonic = _clock.GetTimestampMilliseconds();
        var draftId = current?.DraftId ?? DraftId.New();

        if (!snapshot.IsEmpty)
        {
            var result = current is null
                ? DraftApplyResult.Created
                : current.CurrentSnapshot is not null &&
                  string.Equals(current.CurrentSnapshot.Text, snapshot.Text, StringComparison.Ordinal)
                    ? DraftApplyResult.Unchanged
                    : DraftApplyResult.Updated;
            return (new DraftRecord(
                draftId,
                context,
                snapshot.ContextGeneration,
                snapshot.SnapshotSequence,
                snapshot,
                nowUtc,
                EmptyCandidateState.None,
                null), result);
        }

        if (current is null)
        {
            return (new DraftRecord(
                draftId,
                context,
                snapshot.ContextGeneration,
                snapshot.SnapshotSequence,
                snapshot,
                nowUtc,
                EmptyCandidateState.Started,
                nowMonotonic), DraftApplyResult.EmptyCandidateStarted);
        }

        if (current.EmptyCandidate == EmptyCandidateState.Started &&
            current.EmptyCandidateStartedAtMonotonic is { } startedAt &&
            nowMonotonic - startedAt >= EmptyConfirmationDelayMilliseconds)
        {
            return (current with
            {
                ContextGeneration = snapshot.ContextGeneration,
                LatestAcceptedSnapshotSequence = snapshot.SnapshotSequence,
                CurrentSnapshot = snapshot,
                UpdatedAtUtc = nowUtc,
                EmptyCandidate = EmptyCandidateState.Confirmed,
                EmptyCandidateStartedAtMonotonic = startedAt
            }, DraftApplyResult.EmptyCandidateConfirmed);
        }

        return (current with
        {
            ContextGeneration = snapshot.ContextGeneration,
            LatestAcceptedSnapshotSequence = snapshot.SnapshotSequence,
            UpdatedAtUtc = nowUtc,
            EmptyCandidate = EmptyCandidateState.Started,
            EmptyCandidateStartedAtMonotonic = current.EmptyCandidateStartedAtMonotonic ?? nowMonotonic
        }, DraftApplyResult.EmptyCandidateStarted);
    }
}
