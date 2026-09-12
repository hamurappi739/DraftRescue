namespace DraftRescue.Application.Contracts.Retention;

public interface IRetentionService
{
    Task<int> CleanupExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
}

public interface IWallClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemWallClock : IWallClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
