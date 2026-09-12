using DraftRescue.Application.Models;
using DraftRescue.Domain.Drafts;

namespace DraftRescue.Application.Persistence;

/// <summary>
/// Explicit, measured timing policy for current-state checkpoints. The product
/// does not choose these values implicitly; callers provide the experiment-backed
/// policy when they compose a scheduler.
/// </summary>
public sealed record CheckpointSchedulePolicy
{
    private CheckpointSchedulePolicy(
        TimeSpan trailingDebounce,
        TimeSpan maxDirtyAge,
        TimeSpan retryDelay,
        int maxPendingDrafts)
    {
        TrailingDebounce = trailingDebounce;
        MaxDirtyAge = maxDirtyAge;
        RetryDelay = retryDelay;
        MaxPendingDrafts = maxPendingDrafts;
        TrailingDebounceMilliseconds = ToMilliseconds(trailingDebounce);
        MaxDirtyAgeMilliseconds = ToMilliseconds(maxDirtyAge);
        RetryDelayMilliseconds = ToMilliseconds(retryDelay);
    }

    public TimeSpan TrailingDebounce { get; }
    public TimeSpan MaxDirtyAge { get; }
    public TimeSpan RetryDelay { get; }
    public int MaxPendingDrafts { get; }

    internal long TrailingDebounceMilliseconds { get; }
    internal long MaxDirtyAgeMilliseconds { get; }
    internal long RetryDelayMilliseconds { get; }

    public static CheckpointSchedulePolicy Create(
        TimeSpan trailingDebounce,
        TimeSpan maxDirtyAge,
        TimeSpan retryDelay,
        int maxPendingDrafts)
    {
        if (trailingDebounce <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(trailingDebounce));
        if (maxDirtyAge < trailingDebounce) throw new ArgumentOutOfRangeException(nameof(maxDirtyAge));
        if (retryDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(retryDelay));
        if (maxPendingDrafts <= 0) throw new ArgumentOutOfRangeException(nameof(maxPendingDrafts));
        _ = ToMilliseconds(trailingDebounce);
        _ = ToMilliseconds(maxDirtyAge);
        _ = ToMilliseconds(retryDelay);
        return new CheckpointSchedulePolicy(trailingDebounce, maxDirtyAge, retryDelay, maxPendingDrafts);
    }

    private static long ToMilliseconds(TimeSpan value)
    {
        var milliseconds = value.TotalMilliseconds;
        if (milliseconds <= 0 || milliseconds > long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value));
        return checked((long)Math.Ceiling(milliseconds));
    }
}

public enum CheckpointScheduleOutcome
{
    Scheduled = 0,
    Coalesced = 1,
    StaleIgnored = 2,
    CapacityExceeded = 3,
    SkippedEmpty = 4
}

public readonly record struct CheckpointScheduleResult(
    CheckpointScheduleOutcome Outcome,
    ulong SnapshotSequence);

/// <summary>
/// Bounded current-state checkpoint planner. It does not perform protection or
/// I/O itself; the caller takes due candidates and completes them through
/// <see cref="PersistenceCheckpointCoordinator"/>.
/// </summary>
public sealed class PersistenceCheckpointScheduler : IDisposable
{
    private readonly CheckpointSchedulePolicy _policy;
    private readonly object _gate = new();
    private readonly Dictionary<DraftId, PendingEntry> _pending = new();
    private readonly Dictionary<DraftId, InFlightEntry> _inFlight = new();
    private bool _disposed;

    public PersistenceCheckpointScheduler(CheckpointSchedulePolicy policy)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public int PendingCount
    {
        get
        {
            lock (_gate) return _pending.Count;
        }
    }

    public bool TryGetNextDueAt(out long dueAtMonotonicMilliseconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_gate)
        {
            if (_pending.Count == 0)
            {
                dueAtMonotonicMilliseconds = 0;
                return false;
            }

            dueAtMonotonicMilliseconds = _pending.Values.Min(entry => entry.DueAtMonotonicMilliseconds);
            return true;
        }
    }

    public CheckpointScheduleResult Schedule(CheckpointCandidate candidate, long nowMonotonicMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (candidate.Snapshot.IsEmpty)
            return new CheckpointScheduleResult(CheckpointScheduleOutcome.SkippedEmpty, candidate.Snapshot.SnapshotSequence);

        lock (_gate)
        {
            if (_inFlight.TryGetValue(candidate.DraftId, out var inFlight) &&
                candidate.Snapshot.SnapshotSequence <= inFlight.Candidate.Snapshot.SnapshotSequence)
            {
                return new CheckpointScheduleResult(CheckpointScheduleOutcome.StaleIgnored, candidate.Snapshot.SnapshotSequence);
            }

            if (_pending.TryGetValue(candidate.DraftId, out var existing))
            {
                if (candidate.Snapshot.SnapshotSequence <= existing.Candidate.Snapshot.SnapshotSequence)
                    return new CheckpointScheduleResult(CheckpointScheduleOutcome.StaleIgnored, candidate.Snapshot.SnapshotSequence);

                existing.Candidate = candidate;
                existing.RetryCount = 0;
                existing.DueAtMonotonicMilliseconds = ComputeDueAt(existing.DirtySinceMonotonicMilliseconds, nowMonotonicMilliseconds);
                return new CheckpointScheduleResult(CheckpointScheduleOutcome.Coalesced, candidate.Snapshot.SnapshotSequence);
            }

            if (_pending.Count >= _policy.MaxPendingDrafts)
                return new CheckpointScheduleResult(CheckpointScheduleOutcome.CapacityExceeded, candidate.Snapshot.SnapshotSequence);

            _pending[candidate.DraftId] = new PendingEntry(
                candidate,
                nowMonotonicMilliseconds,
                ComputeDueAt(nowMonotonicMilliseconds, nowMonotonicMilliseconds),
                retryCount: 0);
            return new CheckpointScheduleResult(CheckpointScheduleOutcome.Scheduled, candidate.Snapshot.SnapshotSequence);
        }
    }

    /// <summary>Removes the earliest due candidate and marks it in flight.</summary>
    public bool TryTakeDue(long nowMonotonicMilliseconds, out CheckpointCandidate? candidate)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_gate)
        {
            var due = _pending
                .Where(pair => pair.Value.DueAtMonotonicMilliseconds <= nowMonotonicMilliseconds)
                .OrderBy(pair => pair.Value.DueAtMonotonicMilliseconds)
                .ThenBy(pair => pair.Value.Candidate.Snapshot.SnapshotSequence)
                .FirstOrDefault();

            if (due.Key == default)
            {
                candidate = null;
                return false;
            }

            _pending.Remove(due.Key);
            _inFlight[due.Key] = new InFlightEntry(
                due.Value.Candidate,
                due.Value.DirtySinceMonotonicMilliseconds,
                due.Value.RetryCount);
            candidate = due.Value.Candidate;
            return true;
        }
    }

    /// <summary>
    /// Completes one taken candidate. Protection/repository failures get one
    /// delayed requeue only when no newer candidate is already pending.
    /// </summary>
    public bool Complete(
        CheckpointCandidate candidate,
        CheckpointOutcome outcome,
        long nowMonotonicMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_gate)
        {
            if (!_inFlight.TryGetValue(candidate.DraftId, out var inFlight) ||
                inFlight.Candidate.Snapshot.SnapshotSequence != candidate.Snapshot.SnapshotSequence)
            {
                return false;
            }

            _inFlight.Remove(candidate.DraftId);
            if ((outcome is CheckpointOutcome.ProtectionFailed or CheckpointOutcome.RepositoryFailed) && inFlight.RetryCount == 0)
            {
                if (!_pending.TryGetValue(candidate.DraftId, out var newer))
                {
                    _pending[candidate.DraftId] = new PendingEntry(
                        candidate,
                        inFlight.DirtySinceMonotonicMilliseconds,
                        AddMilliseconds(nowMonotonicMilliseconds, _policy.RetryDelayMilliseconds),
                        retryCount: 1);
                }
                else if (newer.Candidate.Snapshot.SnapshotSequence <= candidate.Snapshot.SnapshotSequence)
                {
                    _pending[candidate.DraftId] = new PendingEntry(
                        candidate,
                        inFlight.DirtySinceMonotonicMilliseconds,
                        AddMilliseconds(nowMonotonicMilliseconds, _policy.RetryDelayMilliseconds),
                        retryCount: 1);
                }
            }

            return true;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _pending.Clear();
            _inFlight.Clear();
        }
    }

    private long ComputeDueAt(long dirtySince, long now)
    {
        var debounceDue = AddMilliseconds(now, _policy.TrailingDebounceMilliseconds);
        var maximumDue = AddMilliseconds(dirtySince, _policy.MaxDirtyAgeMilliseconds);
        return Math.Min(debounceDue, maximumDue);
    }

    private static long AddMilliseconds(long timestamp, long milliseconds) => checked(timestamp + milliseconds);

    private sealed class PendingEntry(
        CheckpointCandidate candidate,
        long dirtySinceMonotonicMilliseconds,
        long dueAtMonotonicMilliseconds,
        int retryCount)
    {
        public CheckpointCandidate Candidate { get; set; } = candidate;
        public long DirtySinceMonotonicMilliseconds { get; } = dirtySinceMonotonicMilliseconds;
        public long DueAtMonotonicMilliseconds { get; set; } = dueAtMonotonicMilliseconds;
        public int RetryCount { get; set; } = retryCount;
    }

    private sealed record InFlightEntry(
        CheckpointCandidate Candidate,
        long DirtySinceMonotonicMilliseconds,
        int RetryCount);
}
