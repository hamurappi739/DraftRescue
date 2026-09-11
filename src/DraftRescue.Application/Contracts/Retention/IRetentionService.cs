namespace DraftRescue.Application.Contracts.Retention;

public interface IRetentionService
{
    Task<int> CleanupExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
}
