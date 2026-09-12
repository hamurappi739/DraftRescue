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

            ValidateSchemaV1(connection);

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

    private static void ValidateSchemaV1(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('drafts');";
        using var reader = command.ExecuteReader();
        var columns = new Dictionary<string, (string Type, bool NotNull, int PrimaryKey)>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            columns[reader.GetString(1)] = (reader.GetString(2), reader.GetInt32(3) != 0, reader.GetInt32(5));
        }

        var expected = new (string Name, string Type, bool NotNull, int PrimaryKey)[]
        {
            ("draft_id", "BLOB", true, 1),
            ("record_schema_version", "INTEGER", true, 0),
            ("application_id", "TEXT", true, 0),
            ("app_profile_id", "TEXT", false, 0),
            ("app_profile_version", "INTEGER", false, 0),
            ("presentation_kind", "INTEGER", true, 0),
            ("fingerprint_version", "INTEGER", true, 0),
            ("match_metadata_version", "INTEGER", true, 0),
            ("match_metadata", "BLOB", true, 0),
            ("protected_payload", "BLOB", true, 0),
            ("protection_version", "INTEGER", true, 0),
            ("snapshot_sequence", "INTEGER", true, 0),
            ("created_at_utc_ms", "INTEGER", true, 0),
            ("updated_at_utc_ms", "INTEGER", true, 0),
            ("expires_at_utc_ms", "INTEGER", true, 0),
            ("recoverable_state", "INTEGER", true, 0)
        };

        if (columns.Count != expected.Length || expected.Any(column =>
                !columns.TryGetValue(column.Name, out var actual) ||
                !string.Equals(actual.Type, column.Type, StringComparison.OrdinalIgnoreCase) ||
                actual.NotNull != column.NotNull ||
                actual.PrimaryKey != column.PrimaryKey))
        {
            throw new SqliteStoreException(SqliteStoreFailureCode.CorruptRecord);
        }

        using var sqlCommand = connection.CreateCommand();
        sqlCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='drafts';";
        var tableSql = Convert.ToString(sqlCommand.ExecuteScalar());
        if (string.IsNullOrWhiteSpace(tableSql) || !tableSql.Contains("WITHOUT ROWID", StringComparison.OrdinalIgnoreCase))
            throw new SqliteStoreException(SqliteStoreFailureCode.CorruptRecord);
    }
}
