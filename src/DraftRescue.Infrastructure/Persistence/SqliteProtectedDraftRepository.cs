using System.Data;
using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Models;
using DraftRescue.Domain.Drafts;
using Microsoft.Data.Sqlite;

namespace DraftRescue.Infrastructure.Persistence;

/// <summary>Single-connection, serialized SQLite repository for protected current-state rows.</summary>
public sealed class SqliteProtectedDraftRepository : IProtectedDraftRepository, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _writer = new(1, 1);
    private readonly SqliteFaultInjector? _faultInjector;
    private bool _disposed;

    public SqliteProtectedDraftRepository(string databasePath, SqliteFaultInjector? faultInjector = null)
    {
        _connection = SqliteStoreBootstrapper.Open(databasePath);
        _faultInjector = faultInjector;
    }

    public async Task<ProtectedDraftUpsertResult> UpsertAsync(ProtectedDraftRecordV1 record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var transaction = _connection.BeginTransaction(IsolationLevel.Serializable);
            var existing = await ReadRowAsync(record.DraftId, transaction, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                await InsertAsync(record, transaction, cancellationToken).ConfigureAwait(false);
                _faultInjector?.ThrowIfInjected(SqliteFaultPoint.BeforeCommit);
                transaction.Commit();
                return ProtectedDraftUpsertResult.Inserted;
            }

            if (record.SnapshotSequence < existing.SnapshotSequence)
            {
                transaction.Rollback();
                return ProtectedDraftUpsertResult.StaleIgnored;
            }

            if (record.SnapshotSequence == existing.SnapshotSequence)
            {
                if (!Equivalent(record, existing))
                    throw new SqliteStoreException(SqliteStoreFailureCode.SequenceConflict);
                transaction.Rollback();
                return ProtectedDraftUpsertResult.IdempotentNoChange;
            }

            await UpdateAsync(record, transaction, cancellationToken).ConfigureAwait(false);
            _faultInjector?.ThrowIfInjected(SqliteFaultPoint.BeforeCommit);
            transaction.Commit();
            return ProtectedDraftUpsertResult.Updated;
        }
        catch (SqliteStoreException) { throw; }
        catch (SqliteException ex) { throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable, ex); }
        finally { _writer.Release(); }
    }

    public async Task<ProtectedDraftRecordV1?> GetProtectedAsync(DraftId draftId, CancellationToken cancellationToken = default)
    {
        await EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadRowAsync(draftId, transaction: null, cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteStoreException) { throw; }
        catch (SqliteException ex) { throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable, ex); }
        finally { _writer.Release(); }
    }

    public async Task<IReadOnlyList<RecoverableDraftMetadata>> ListRecoverableMetadataAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        await EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
SELECT draft_id, application_id, presentation_kind, updated_at_utc_ms, expires_at_utc_ms, recoverable_state
FROM drafts
WHERE expires_at_utc_ms > $now AND recoverable_state = 0
ORDER BY updated_at_utc_ms DESC;
""";
            command.Parameters.AddWithValue("$now", ToUnixMilliseconds(nowUtc));
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
            var result = new List<RecoverableDraftMetadata>();
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    result.Add(new RecoverableDraftMetadata(
                        new DraftId(new Guid((byte[])reader[0])),
                        reader.GetString(1),
                        ReadPresentationKind(reader.GetInt32(2)),
                        FromUnixMilliseconds(reader.GetInt64(3)),
                        FromUnixMilliseconds(reader.GetInt64(4)),
                        ReadRecoverableState(reader.GetInt32(5))));
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or InvalidCastException or FormatException or OverflowException)
                {
                    throw new SqliteStoreException(SqliteStoreFailureCode.CorruptRecord, ex);
                }
            }
            return result;
        }
        catch (SqliteException ex) { throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable, ex); }
        finally { _writer.Release(); }
    }

    public async Task DeleteAsync(DraftId draftId, CancellationToken cancellationToken = default)
    {
        await ExecuteDeleteAsync("DELETE FROM drafts WHERE draft_id = $id;", draftId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> DeleteExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        await EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var transaction = _connection.BeginTransaction(IsolationLevel.Serializable);
            using var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM drafts WHERE expires_at_utc_ms <= $now;";
            command.Parameters.AddWithValue("$now", ToUnixMilliseconds(nowUtc));
            var count = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            transaction.Commit();
            return count;
        }
        catch (SqliteException ex) { throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable, ex); }
        finally { _writer.Release(); }
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var transaction = _connection.BeginTransaction(IsolationLevel.Serializable);
            using var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM drafts;";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            transaction.Commit();
        }
        catch (SqliteException ex) { throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable, ex); }
        finally { _writer.Release(); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
        _writer.Dispose();
    }

    private async Task ExecuteDeleteAsync(string sql, DraftId draftId, CancellationToken cancellationToken)
    {
        await EnterAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("$id", draftId.Value.ToByteArray());
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex) { throw new SqliteStoreException(SqliteStoreFailureCode.Unavailable, ex); }
        finally { _writer.Release(); }
    }

    private async Task EnterAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _writer.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProtectedDraftRecordV1?> ReadRowAsync(DraftId draftId, SqliteTransaction? transaction, CancellationToken cancellationToken)
    {
        using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
SELECT draft_id, record_schema_version, application_id, app_profile_id, app_profile_version,
 presentation_kind, fingerprint_version, match_metadata_version, match_metadata, protected_payload,
 protection_version, snapshot_sequence, created_at_utc_ms, updated_at_utc_ms, expires_at_utc_ms, recoverable_state
FROM drafts WHERE draft_id = $id;
""";
        command.Parameters.AddWithValue("$id", draftId.Value.ToByteArray());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
        try
        {
            var payload = ProtectedDraftPayload.Create(reader.GetInt32(10), (byte[])reader[9]);
            return ProtectedDraftRecordV1.Create(
                new DraftId(new Guid((byte[])reader[0])), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4), ReadPresentationKind(reader.GetInt32(5)), reader.GetInt32(6),
                reader.GetInt32(7), (byte[])reader[8], payload, reader.GetInt64(11), FromUnixMilliseconds(reader.GetInt64(12)),
                FromUnixMilliseconds(reader.GetInt64(13)), FromUnixMilliseconds(reader.GetInt64(14)), ReadRecoverableState(reader.GetInt32(15)));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or InvalidCastException or FormatException or OverflowException)
        {
            throw new SqliteStoreException(SqliteStoreFailureCode.CorruptRecord, ex);
        }
    }

    private async Task InsertAsync(ProtectedDraftRecordV1 record, SqliteTransaction transaction, CancellationToken cancellationToken)
        => await ExecuteMutationAsync(record, transaction, cancellationToken, insert: true).ConfigureAwait(false);

    private async Task UpdateAsync(ProtectedDraftRecordV1 record, SqliteTransaction transaction, CancellationToken cancellationToken)
        => await ExecuteMutationAsync(record, transaction, cancellationToken, insert: false).ConfigureAwait(false);

    private async Task ExecuteMutationAsync(ProtectedDraftRecordV1 record, SqliteTransaction transaction, CancellationToken cancellationToken, bool insert)
    {
        using var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = insert
            ? """
INSERT INTO drafts (draft_id, record_schema_version, application_id, app_profile_id, app_profile_version,
 presentation_kind, fingerprint_version, match_metadata_version, match_metadata, protected_payload,
 protection_version, snapshot_sequence, created_at_utc_ms, updated_at_utc_ms, expires_at_utc_ms, recoverable_state)
VALUES ($id, 1, $app, $profile, $profileVersion, $kind, $fingerprintVersion, $metadataVersion, $metadata, $payload,
 $protectionVersion, $sequence, $created, $updated, $expires, $state);
"""
            : """
UPDATE drafts SET application_id=$app, app_profile_id=$profile, app_profile_version=$profileVersion,
 presentation_kind=$kind, fingerprint_version=$fingerprintVersion, match_metadata_version=$metadataVersion,
 match_metadata=$metadata, protected_payload=$payload, protection_version=$protectionVersion,
 snapshot_sequence=$sequence, created_at_utc_ms=$created, updated_at_utc_ms=$updated,
 expires_at_utc_ms=$expires, recoverable_state=$state WHERE draft_id=$id;
""";
        command.Parameters.AddWithValue("$id", record.DraftId.Value.ToByteArray());
        command.Parameters.AddWithValue("$app", record.ApplicationId);
        command.Parameters.AddWithValue("$profile", (object?)record.AppProfileId ?? DBNull.Value);
        command.Parameters.AddWithValue("$profileVersion", (object?)record.AppProfileVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("$kind", (int)record.PresentationKind);
        command.Parameters.AddWithValue("$fingerprintVersion", record.FingerprintVersion);
        command.Parameters.AddWithValue("$metadataVersion", record.MatchMetadataVersion);
        command.Parameters.AddWithValue("$metadata", record.MatchMetadataBytes.ToArray());
        command.Parameters.AddWithValue("$payload", record.ProtectedPayloadBytes.ToArray());
        command.Parameters.AddWithValue("$protectionVersion", record.ProtectionVersion);
        command.Parameters.AddWithValue("$sequence", record.SnapshotSequence);
        command.Parameters.AddWithValue("$created", ToUnixMilliseconds(record.CreatedAtUtc));
        command.Parameters.AddWithValue("$updated", ToUnixMilliseconds(record.UpdatedAtUtc));
        command.Parameters.AddWithValue("$expires", ToUnixMilliseconds(record.ExpiresAtUtc));
        command.Parameters.AddWithValue("$state", (int)record.RecoverableState);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool Equivalent(ProtectedDraftRecordV1 left, ProtectedDraftRecordV1 right) =>
        left.ApplicationId == right.ApplicationId && left.AppProfileId == right.AppProfileId && left.AppProfileVersion == right.AppProfileVersion &&
        left.PresentationKind == right.PresentationKind && left.FingerprintVersion == right.FingerprintVersion && left.MatchMetadataVersion == right.MatchMetadataVersion &&
        left.MatchMetadataBytes.Span.SequenceEqual(right.MatchMetadataBytes.Span) && left.ProtectedPayloadBytes.Span.SequenceEqual(right.ProtectedPayloadBytes.Span) &&
        left.ProtectionVersion == right.ProtectionVersion && ToUnixMilliseconds(left.CreatedAtUtc) == ToUnixMilliseconds(right.CreatedAtUtc) &&
        ToUnixMilliseconds(left.UpdatedAtUtc) == ToUnixMilliseconds(right.UpdatedAtUtc) && ToUnixMilliseconds(left.ExpiresAtUtc) == ToUnixMilliseconds(right.ExpiresAtUtc) &&
        left.RecoverableState == right.RecoverableState;

    private static long ToUnixMilliseconds(DateTimeOffset value) => value.ToUniversalTime().ToUnixTimeMilliseconds();
    private static DateTimeOffset FromUnixMilliseconds(long value) => DateTimeOffset.FromUnixTimeMilliseconds(value);

    private static DraftPresentationKind ReadPresentationKind(int value) =>
        Enum.IsDefined((DraftPresentationKind)value)
            ? (DraftPresentationKind)value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown presentation kind.");

    private static RecoverableState ReadRecoverableState(int value) =>
        Enum.IsDefined((RecoverableState)value)
            ? (RecoverableState)value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown recoverable state.");
}
