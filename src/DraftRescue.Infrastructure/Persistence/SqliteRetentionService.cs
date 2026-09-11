using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Contracts.Retention;

namespace DraftRescue.Infrastructure.Persistence;

/// <summary>Metadata-only expiry execution; it never opens or decrypts a payload.</summary>
public sealed class SqliteRetentionService : IRetentionService
{
    private readonly IProtectedDraftRepository _repository;

    public SqliteRetentionService(IProtectedDraftRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<int> CleanupExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default) =>
        _repository.DeleteExpiredAsync(nowUtc, cancellationToken);
}
