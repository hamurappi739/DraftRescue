using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Contracts.Retention;
using DraftRescue.Application.Persistence;
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
        _checkpointCoordinator.Dispose();
        await _retention.DisposeAsync().ConfigureAwait(false);
        _repository.Dispose();
    }
}
