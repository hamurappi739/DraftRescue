using DraftRescue.Application.Models;
using DraftRescue.Application.Security;

namespace DraftRescue.Application.Persistence;

/// <summary>
/// Bounded executor for <see cref="PersistenceCheckpointScheduler"/>. It owns
/// no plaintext queue beyond the scheduler's single newest candidate per draft
/// and delegates every durable write to the protect-before-repository coordinator.
/// </summary>
public sealed class PersistenceCheckpointWorker : IAsyncDisposable
{
    private readonly PersistenceCheckpointScheduler _scheduler;
    private readonly PersistenceCheckpointCoordinator _coordinator;
    private readonly IMonotonicClock _clock;
    private readonly SemaphoreSlim _wake = new(0, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _loop;
    private int _started;
    private int _running;
    private int _disposed;

    public PersistenceCheckpointWorker(
        PersistenceCheckpointScheduler scheduler,
        PersistenceCheckpointCoordinator coordinator,
        IMonotonicClock clock)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public bool IsRunning => Volatile.Read(ref _running) != 0;

    public CheckpointScheduleResult Schedule(CheckpointCandidate candidate)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var result = _scheduler.Schedule(candidate, _clock.GetTimestampMilliseconds());
        SignalWake();
        return result;
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.Exchange(ref _started, 1) != 0)
            throw new InvalidOperationException("Checkpoint worker is already started.");

        var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        Volatile.Write(ref _running, 1);
        _loop = RunAsync(linked);
    }

    /// <summary>Processes all candidates currently due at the injected clock time.</summary>
    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var processed = 0;
        while (_scheduler.TryTakeDue(_clock.GetTimestampMilliseconds(), out var candidate))
        {
            var result = await _coordinator.CheckpointAsync(candidate!, cancellationToken).ConfigureAwait(false);
            _scheduler.Complete(candidate!, result.Outcome, _clock.GetTimestampMilliseconds());
            processed++;
        }

        if (_scheduler.TryGetNextDueAt(out _)) SignalWake();
        return processed;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        SignalWake();
        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _wake.Dispose();
        _lifetime.Dispose();
    }

    private async Task RunAsync(CancellationTokenSource linked)
    {
        try
        {
            while (!linked.IsCancellationRequested)
            {
                await ProcessDueAsync(linked.Token).ConfigureAwait(false);
                if (linked.IsCancellationRequested) break;
                if (!await WaitForNextDueOrSignalAsync(linked.Token).ConfigureAwait(false)) break;
            }
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (linked.IsCancellationRequested || Volatile.Read(ref _disposed) != 0) { }
        finally
        {
            Volatile.Write(ref _running, 0);
            linked.Dispose();
        }
    }

    private async Task<bool> WaitForNextDueOrSignalAsync(CancellationToken cancellationToken)
    {
        if (!_scheduler.TryGetNextDueAt(out var dueAt))
        {
            await _wake.WaitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        var remaining = dueAt - _clock.GetTimestampMilliseconds();
        if (remaining <= 0) return true;
        var delay = Task.Delay(TimeSpan.FromMilliseconds(Math.Min(remaining, 60_000L)), cancellationToken);
        var signal = _wake.WaitAsync(cancellationToken);
        await Task.WhenAny(delay, signal).ConfigureAwait(false);
        return true;
    }

    private void SignalWake()
    {
        try { _wake.Release(); }
        catch (SemaphoreFullException) { }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0) { }
    }
}
