using Microsoft.Data.Sqlite;

namespace DraftRescue.Infrastructure.Persistence;

public static class SqliteStoreBootstrapper
{
    public static SqliteConnection Open(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false
        }.ToString());
        try
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            connection.Open();
            Execute(connection, "PRAGMA journal_mode=DELETE;");
            Execute(connection, "PRAGMA secure_delete=ON;");
            Execute(connection, "PRAGMA foreign_keys=ON;");
            Execute(connection, "PRAGMA busy_timeout=1500;");
            Execute(connection, "PRAGMA synchronous=EXTRA;");

            if (!string.Equals(Convert.ToString(Scalar(connection, "PRAGMA journal_mode;")), "delete", StringComparison.OrdinalIgnoreCase) ||
                Convert.ToInt32(Scalar(connection, "PRAGMA secure_delete;")) != 1 ||
                Convert.ToInt32(Scalar(connection, "PRAGMA foreign_keys;")) != 1 ||
                Convert.ToInt32(Scalar(connection, "PRAGMA busy_timeout;")) != 1500 ||
                Convert.ToInt32(Scalar(connection, "PRAGMA synchronous;")) != 3)
            {
                throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable);
            }

            var version = Convert.ToInt32(Scalar(connection, "PRAGMA user_version;"));
            if (version == 0)
            {
                using var transaction = connection.BeginTransaction();
                Execute(connection, SqliteSchemaV1.CreateDraftsTableSql, transaction);
                Execute(connection, SqliteSchemaV1.CreateExpiryIndexSql, transaction);
                Execute(connection, "PRAGMA user_version=1;", transaction);
                transaction.Commit();
            }
            else if (version != SqliteSchemaV1.UserVersion)
            {
                throw new SqliteStoreException(SqliteStoreFailureCode.IncompatibleSchema);
            }

            return connection;
        }
        catch (SqliteStoreException)
        {
            connection.Dispose();
            throw;
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            connection.Dispose();
            throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable, ex);
        }
    }

    private static object? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.ExecuteNonQuery();
    }
}
