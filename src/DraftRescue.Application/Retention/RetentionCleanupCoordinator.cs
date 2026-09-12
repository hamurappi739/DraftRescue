using DraftRescue.Application.Contracts.Retention;

namespace DraftRescue.Application.Retention;

/// <summary>
/// Runs metadata-only expiry cleanup at startup and at a low frequency while the
/// process is alive. A failed cleanup gets one bounded retry; no exception text
/// or draft data is retained in diagnostics.
/// </summary>
public sealed class RetentionCleanupCoordinator : IAsyncDisposable
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

    private readonly IRetentionService _service;
    private readonly IWallClock _clock;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _retryDelay;
    private readonly CancellationTokenSource _lifetime = new();
    private Task? _runtimeTask;
    private int _started;
    private int _disposed;
    private long _attemptCount;
    private long _failureCount;
    private int _lastDeletedCount;

    public RetentionCleanupCoordinator(
        IRetentionService service,
        IWallClock clock,
        TimeSpan? interval = null,
        TimeSpan? retryDelay = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _interval = interval ?? DefaultInterval;
        _retryDelay = retryDelay ?? DefaultRetryDelay;
        if (_interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
        if (_retryDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(retryDelay));
    }

    public RetentionCleanupHealth Health => new(
        Interlocked.Read(ref _attemptCount),
        Interlocked.Read(ref _failureCount),
        Volatile.Read(ref _lastDeletedCount));

    /// <summary>Runs startup cleanup before a recovery list is exposed.</summary>
    public Task<int> CleanupOnStartupAsync(CancellationToken cancellationToken = default) =>
        ExecuteBoundedAsync(cancellationToken);

    /// <summary>Starts low-frequency runtime cleanup. Startup cleanup is explicit and separate.</summary>
    public void Start(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.Exchange(ref _started, 1) != 0)
            throw new InvalidOperationException("Retention cleanup is already started.");

        var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
        _runtimeTask = RunAsync(linked);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        if (_runtimeTask is not null)
        {
            try { await _runtimeTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
        _lifetime.Dispose();
    }

    private async Task RunAsync(CancellationTokenSource linked)
    {
        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(linked.Token).ConfigureAwait(false))
                await ExecuteBoundedAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested) { }
        finally { linked.Dispose(); }
    }

    private async Task<int> ExecuteBoundedAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _attemptCount);
            try
            {
                var deleted = await _service.CleanupExpiredAsync(_clock.UtcNow, cancellationToken).ConfigureAwait(false);
                Volatile.Write(ref _lastDeletedCount, deleted);
                return deleted;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch
            {
                Interlocked.Increment(ref _failureCount);
                if (attempt++ != 0) return 0;
                if (_retryDelay > TimeSpan.Zero)
                    await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

public readonly record struct RetentionCleanupHealth(
    long AttemptCount,
    long FailureCount,
    int LastDeletedCount);
