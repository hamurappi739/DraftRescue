using Microsoft.Data.Sqlite;

namespace DraftRescue.Infrastructure.Persistence;

public enum StoreOpenResult
{
    Ready = 0,
    Incompatible = 1,
    Corrupt = 2,
    Unavailable = 3
}

/// <summary>Fail-closed classification and local-only quarantine. It never salvages or recreates data.</summary>
public static class SqliteStoreRecovery
{
    public static StoreOpenResult Classify(Exception exception) => exception switch
    {
        SqliteStoreException { Code: SqliteStoreFailureCode.IncompatibleSchema } => StoreOpenResult.Incompatible,
        SqliteStoreException { Code: SqliteStoreFailureCode.CorruptRecord } => StoreOpenResult.Corrupt,
        SqliteException sqlite when sqlite.SqliteErrorCode is 11 or 26 => StoreOpenResult.Corrupt,
        _ => StoreOpenResult.Unavailable
    };

    public static string? Quarantine(string databasePath, string quarantineDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(quarantineDirectory);
        if (!File.Exists(databasePath)) return null;

        Directory.CreateDirectory(quarantineDirectory);
        var name = Path.GetFileNameWithoutExtension(databasePath);
        var destination = Path.Combine(quarantineDirectory, $"{name}.quarantine-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.db");
        try
        {
            File.Move(databasePath, destination, overwrite: false);
            return destination;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

public static class SqliteMigrationPolicy
{
    public const int CurrentVersion = 1;

    public static bool IsSupported(int userVersion) => userVersion == CurrentVersion;

    public static bool RequiresKnownMigration(int userVersion) => userVersion > 0 && userVersion != CurrentVersion;
}
