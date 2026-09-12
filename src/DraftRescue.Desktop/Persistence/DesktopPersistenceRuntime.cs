using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Contracts.Retention;
using DraftRescue.Application.Models;
using DraftRescue.Application.Persistence;
using DraftRescue.Application.Security;
using DraftRescue.Application.Retention;
using DraftRescue.Infrastructure.Persistence;
using DraftRescue.Platform.Windows.Security;
using DraftRescue.Platform.Windows.Storage;

namespace DraftRescue.Desktop.Persistence;

public enum DesktopPersistenceAvailability
{
    Ready = 0,
    Unavailable = 1,
    Incompatible = 2,
    Corrupt = 3
}

public sealed record DesktopPersistenceStartupResult(
    DesktopPersistenceAvailability Availability,
    DesktopPersistenceRuntime? Runtime);

/// <summary>Desktop composition root for local protected persistence and retention.</summary>
public sealed class DesktopPersistenceRuntime : IAsyncDisposable
{
    private readonly SqliteProtectedDraftRepository _repository;
    private readonly SqliteRetentionService _retentionService;
    private readonly RetentionCleanupCoordinator _retention;
    private readonly PersistenceCheckpointCoordinator _checkpointCoordinator;
    private readonly object _checkpointWorkerGate = new();
    private PersistenceCheckpointScheduler? _checkpointScheduler;
    private PersistenceCheckpointWorker? _checkpointWorker;
    private int _disposed;

    private DesktopPersistenceRuntime(
        SqliteProtectedDraftRepository repository,
        SqliteRetentionService retentionService,
        RetentionCleanupCoordinator retention,
        PersistenceCheckpointCoordinator checkpointCoordinator)
    {
        _repository = repository;
        _retentionService = retentionService;
        _retention = retention;
        _checkpointCoordinator = checkpointCoordinator;
    }

    public IProtectedDraftRepository Repository => _repository;
    public IRetentionService Retention => _retentionService;
    public PersistenceCheckpointCoordinator CheckpointCoordinator => _checkpointCoordinator;
    public RetentionCleanupHealth RetentionHealth => _retention.Health;
    public bool IsCheckpointWorkerRunning => _checkpointWorker?.IsRunning == true;

    /// <summary>
    /// Starts the optional current-state checkpoint executor. Timing is an
    /// explicit caller decision; this composition root never invents product
    /// debounce/max-age defaults while Open Decision 6 remains unresolved.
    /// </summary>
    public void StartCheckpointWorker(
        CheckpointSchedulePolicy policy,
        IMonotonicClock clock,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(clock);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        lock (_checkpointWorkerGate)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (_checkpointWorker is not null)
                throw new InvalidOperationException("Checkpoint worker is already started.");

            var scheduler = new PersistenceCheckpointScheduler(policy);
            var worker = new PersistenceCheckpointWorker(scheduler, _checkpointCoordinator, clock);
            try
            {
                worker.Start(cancellationToken);
            }
            catch
            {
                scheduler.Dispose();
                worker.DisposeAsync().AsTask().GetAwaiter().GetResult();
                throw;
            }

            _checkpointScheduler = scheduler;
            _checkpointWorker = worker;
        }
    }

    public CheckpointScheduleResult ScheduleCheckpoint(CheckpointCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        lock (_checkpointWorkerGate)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            return (_checkpointWorker ?? throw new InvalidOperationException("Checkpoint worker is not started.")).Schedule(candidate);
        }
    }

    public static async Task<DesktopPersistenceStartupResult> StartDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        var paths = new WindowsLocalDataPathProvider();
        return await StartAsync(
            paths,
            new SystemWallClock(),
            new WindowsDpapiInstallationSecretStore(paths.InstallationSecretPath),
            new WindowsDpapiDraftProtector(),
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DesktopPersistenceStartupResult> StartAsync(
        IAppDataPathProvider paths,
        IWallClock clock,
        CancellationToken cancellationToken = default)
        => await StartAsync(
            paths,
            clock,
            new WindowsDpapiInstallationSecretStore(paths.InstallationSecretPath),
            new WindowsDpapiDraftProtector(),
            cancellationToken).ConfigureAwait(false);

    public static async Task<DesktopPersistenceStartupResult> StartAsync(
        IAppDataPathProvider paths,
        IWallClock clock,
        IInstallationSecretProvider installationSecrets,
        IDraftProtector protector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(installationSecrets);
        ArgumentNullException.ThrowIfNull(protector);
        SqliteProtectedDraftRepository? repository = null;
        SqliteRetentionService? retentionService = null;
        RetentionCleanupCoordinator? retention = null;
        PersistenceCheckpointCoordinator? checkpointCoordinator = null;
        try
        {
            // Verify the per-user DPAPI-backed identity before opening or creating
            // the SQLite store. A missing/invalid secret must never silently create
            // a new identity next to an existing database.
            _ = await installationSecrets.GetOrCreateAsync(cancellationToken).ConfigureAwait(false);
            repository = new SqliteProtectedDraftRepository(paths.DatabasePath);
            retentionService = new SqliteRetentionService(repository);
            retention = new RetentionCleanupCoordinator(retentionService, clock);
            checkpointCoordinator = new PersistenceCheckpointCoordinator(protector, repository);
            await retention.CleanupOnStartupAsync(cancellationToken).ConfigureAwait(false);
            retention.Start(cancellationToken);
            var runtime = new DesktopPersistenceRuntime(repository, retentionService, retention, checkpointCoordinator);
            repository = null;
            retentionService = null;
            retention = null;
            checkpointCoordinator = null;
            return new DesktopPersistenceStartupResult(DesktopPersistenceAvailability.Ready, runtime);
        }
        catch (DraftProtectionException)
        {
            if (checkpointCoordinator is not null) checkpointCoordinator.Dispose();
            if (retention is not null) await retention.DisposeAsync().ConfigureAwait(false);
            repository?.Dispose();
            return new DesktopPersistenceStartupResult(DesktopPersistenceAvailability.Unavailable, null);
        }
        catch (SqliteStoreException error)
        {
            var availability = SqliteStoreRecovery.Classify(error) switch
            {
                StoreOpenResult.Incompatible => DesktopPersistenceAvailability.Incompatible,
                StoreOpenResult.Corrupt => DesktopPersistenceAvailability.Corrupt,
                _ => DesktopPersistenceAvailability.Unavailable
            };
            checkpointCoordinator?.Dispose();
            if (retention is not null) await retention.DisposeAsync().ConfigureAwait(false);
            repository?.Dispose();
            return new DesktopPersistenceStartupResult(availability, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            checkpointCoordinator?.Dispose();
            if (retention is not null) await retention.DisposeAsync().ConfigureAwait(false);
            repository?.Dispose();
            throw;
        }
        catch
        {
            checkpointCoordinator?.Dispose();
            if (retention is not null) await retention.DisposeAsync().ConfigureAwait(false);
            repository?.Dispose();
            return new DesktopPersistenceStartupResult(DesktopPersistenceAvailability.Unavailable, null);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        PersistenceCheckpointWorker? checkpointWorker;
        PersistenceCheckpointScheduler? checkpointScheduler;
        lock (_checkpointWorkerGate)
        {
            checkpointWorker = _checkpointWorker;
            checkpointScheduler = _checkpointScheduler;
            _checkpointWorker = null;
            _checkpointScheduler = null;
        }

        if (checkpointWorker is not null)
            await checkpointWorker.DisposeAsync().ConfigureAwait(false);
        checkpointScheduler?.Dispose();
        _checkpointCoordinator.Dispose();
        await _retention.DisposeAsync().ConfigureAwait(false);
        _repository.Dispose();
    }
}
