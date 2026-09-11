namespace DraftRescue.Infrastructure.Persistence;

public enum SqliteFaultPoint
{
    BeforeCommit = 1
}

/// <summary>Test-only deterministic fault boundary. It never writes or exposes draft content.</summary>
public sealed class SqliteFaultInjector
{
    public SqliteFaultInjector(SqliteFaultPoint point) => Point = point;

    public SqliteFaultPoint Point { get; }

    public void ThrowIfInjected(SqliteFaultPoint point)
    {
        if (Point == point) throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable);
    }
}
