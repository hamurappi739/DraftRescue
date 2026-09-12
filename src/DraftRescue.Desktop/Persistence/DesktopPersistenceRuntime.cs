using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Contracts.Retention;
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
    private readonly RetentionCleanupCoordinator _retention;
    private int _disposed;

    private DesktopPersistenceRuntime(
        SqliteProtectedDraftRepository repository,
        RetentionCleanupCoordinator retention)
    {
        _repository = repository;
        _retention = retention;
    }

    public IProtectedDraftRepository Repository => _repository;
    public IRetentionService Retention => new SqliteRetentionService(_repository);
    public RetentionCleanupHealth RetentionHealth => _retention.Health;

    public static async Task<DesktopPersistenceStartupResult> StartDefaultAsync(
        CancellationToken cancellationToken = default)
        => await StartAsync(new WindowsLocalDataPathProvider(), new SystemWallClock(), cancellationToken).ConfigureAwait(false);

    public static async Task<DesktopPersistenceStartupResult> StartAsync(
        IAppDataPathProvider paths,
        IWallClock clock,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(clock);
        SqliteProtectedDraftRepository? repository = null;
        RetentionCleanupCoordinator? retention = null;
        try
        {
            repository = new SqliteProtectedDraftRepository(paths.DatabasePath);
            var service = new SqliteRetentionService(repository);
            retention = new RetentionCleanupCoordinator(service, clock);
            await retention.CleanupOnStartupAsync(cancellationToken).ConfigureAwait(false);
            retention.Start(cancellationToken);
            var runtime = new DesktopPersistenceRuntime(repository, retention);
            repository = null;
            retention = null;
            return new DesktopPersistenceStartupResult(DesktopPersistenceAvailability.Ready, runtime);
        }
        catch (SqliteStoreException error)
        {
            var availability = SqliteStoreRecovery.Classify(error) switch
            {
                StoreOpenResult.Incompatible => DesktopPersistenceAvailability.Incompatible,
                StoreOpenResult.Corrupt => DesktopPersistenceAvailability.Corrupt,
                _ => DesktopPersistenceAvailability.Unavailable
            };
            if (retention is not null) await retention.DisposeAsync().ConfigureAwait(false);
            repository?.Dispose();
            return new DesktopPersistenceStartupResult(availability, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (retention is not null) await retention.DisposeAsync().ConfigureAwait(false);
            repository?.Dispose();
            throw;
        }
        catch
        {
            if (retention is not null) await retention.DisposeAsync().ConfigureAwait(false);
            repository?.Dispose();
            return new DesktopPersistenceStartupResult(DesktopPersistenceAvailability.Unavailable, null);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await _retention.DisposeAsync().ConfigureAwait(false);
        _repository.Dispose();
    }
}
