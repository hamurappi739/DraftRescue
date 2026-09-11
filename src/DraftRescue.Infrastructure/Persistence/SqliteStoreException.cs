namespace DraftRescue.Infrastructure.Persistence;

public enum SqliteStoreFailureCode
{
    IncompatibleSchema = 1,
    CorruptRecord = 2,
    Unavailable = 3,
    SequenceConflict = 4
}

public sealed class SqliteStoreException : Exception
{
    public SqliteStoreException(SqliteStoreFailureCode code, Exception? innerException = null)
        : base(code.ToString(), innerException) => Code = code;

    public SqliteStoreFailureCode Code { get; }
}
