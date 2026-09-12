using DraftRescue.Application.Models;
using DraftRescue.Domain.Drafts;
using DraftRescue.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4SqliteTests
{
    [Fact]
    public async Task BootstrapCreatesCanonicalSchemaAndPragmas()
    {
        var (directory, path) = TempStore();
        try
        {
            using var repository = new SqliteProtectedDraftRepository(path);
            await using var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='drafts';";
            var sql = (string?)await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Contains("WITHOUT ROWID", sql, StringComparison.Ordinal);
            Assert.Contains("protected_payload BLOB NOT NULL", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("preview", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("snippet", sql, StringComparison.OrdinalIgnoreCase);
            await connection.DisposeAsync();
            repository.Dispose();
        }
        finally { DeleteTemp(directory); }
    }

    [Fact]
    public async Task UpsertEnforcesMonotonicSequenceAndIdempotency()
    {
        var (directory, path) = TempStore();
        try
        {
            using var repository = new SqliteProtectedDraftRepository(path);
            var id = DraftId.New();
            var first = Record(id, 10, "first");
            var newer = Record(id, 11, "newer");
            Assert.Equal(ProtectedDraftUpsertResult.Inserted, await repository.UpsertAsync(first, TestContext.Current.CancellationToken));
            Assert.Equal(ProtectedDraftUpsertResult.Updated, await repository.UpsertAsync(newer, TestContext.Current.CancellationToken));
            Assert.Equal(ProtectedDraftUpsertResult.StaleIgnored, await repository.UpsertAsync(first, TestContext.Current.CancellationToken));
            Assert.Equal(ProtectedDraftUpsertResult.IdempotentNoChange, await repository.UpsertAsync(newer, TestContext.Current.CancellationToken));
            var loaded = await repository.GetProtectedAsync(id, TestContext.Current.CancellationToken);
            Assert.NotNull(loaded);
            Assert.Equal(11, loaded!.SnapshotSequence);
            Assert.Equal(new byte[] { 11 }, loaded.ProtectedPayloadBytes.ToArray());
            repository.Dispose();
        }
        finally { DeleteTemp(directory); }
    }

    [Fact]
    public async Task EqualSequenceWithDifferentPayloadFailsAsConflict()
    {
        var (directory, path) = TempStore();
        try
        {
            using var repository = new SqliteProtectedDraftRepository(path);
            var id = DraftId.New();
            await repository.UpsertAsync(Record(id, 1, "a"), TestContext.Current.CancellationToken);
            var error = await Assert.ThrowsAsync<SqliteStoreException>(() => repository.UpsertAsync(Record(id, 1, "b"), TestContext.Current.CancellationToken));
            Assert.Equal(SqliteStoreFailureCode.SequenceConflict, error.Code);
            var loaded = await repository.GetProtectedAsync(id, TestContext.Current.CancellationToken);
            Assert.Equal(new byte[] { 1 }, loaded!.ProtectedPayloadBytes.ToArray());
            repository.Dispose();
        }
        finally { DeleteTemp(directory); }
    }

    [Fact]
    public async Task MetadataListExpiryAndDeletesNeverRequirePayloadAccess()
    {
        var (directory, path) = TempStore();
        try
        {
            using var repository = new SqliteProtectedDraftRepository(path);
            var now = DateTimeOffset.UtcNow;
            var liveId = DraftId.New();
            await repository.UpsertAsync(Record(liveId, 1, "live", now.AddMinutes(5)), TestContext.Current.CancellationToken);
            await repository.UpsertAsync(Record(DraftId.New(), 2, "expired", now.AddMinutes(-1)), TestContext.Current.CancellationToken);
            var list = await repository.ListRecoverableMetadataAsync(now, TestContext.Current.CancellationToken);
            var item = Assert.Single(list);
            Assert.Equal(liveId, item.DraftId);
            Assert.Equal("live", item.ApplicationId);
            Assert.Equal(1, await repository.DeleteExpiredAsync(now, TestContext.Current.CancellationToken));
            await repository.DeleteAsync(liveId, TestContext.Current.CancellationToken);
            Assert.Empty(await repository.ListRecoverableMetadataAsync(now, TestContext.Current.CancellationToken));
            repository.Dispose();
        }
        finally { DeleteTemp(directory); }
    }

    [Fact]
    public async Task SerializedConcurrentWritesLeaveHighestSequence()
    {
        var (directory, path) = TempStore();
        try
        {
            using var repository = new SqliteProtectedDraftRepository(path);
            var id = DraftId.New();
            var token = TestContext.Current.CancellationToken;
            var tasks = Enumerable.Range(1, 20).Select(sequence => repository.UpsertAsync(Record(id, sequence, sequence.ToString()), token));
            await Task.WhenAll(tasks);
            var loaded = await repository.GetProtectedAsync(id, token);
            Assert.Equal(20, loaded!.SnapshotSequence);
            repository.Dispose();
        }
        finally { DeleteTemp(directory); }
    }

    [Fact]
    public void UnknownSchemaVersionFailsClosed()
    {
        var (directory, path) = TempStore();
        try
        {
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=false"))
            {
                Directory.CreateDirectory(directory);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA user_version=99;";
                command.ExecuteNonQuery();
            }
            var error = Assert.Throws<SqliteStoreException>(() => new SqliteProtectedDraftRepository(path));
            Assert.Equal(SqliteStoreFailureCode.IncompatibleSchema, error.Code);
        }
        finally { DeleteTemp(directory); }
    }

    [Fact]
    public void SchemaShapeMismatchFailsClosedWithoutRebuild()
    {
        var (directory, path) = TempStore();
        try
        {
            Directory.CreateDirectory(directory);
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=false"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE drafts (draft_id BLOB NOT NULL PRIMARY KEY) WITHOUT ROWID; PRAGMA user_version=1;";
                command.ExecuteNonQuery();
            }

            var error = Assert.Throws<SqliteStoreException>(() => new SqliteProtectedDraftRepository(path));
            Assert.Equal(SqliteStoreFailureCode.CorruptRecord, error.Code);
            Assert.True(File.Exists(path));
        }
        finally { DeleteTemp(directory); }
    }

    private static ProtectedDraftRecordV1 Record(DraftId id, long sequence, string application, DateTimeOffset? expiry = null)
    {
        var updated = expiry?.AddMinutes(-1) ?? DateTimeOffset.UtcNow;
        return ProtectedDraftRecordV1.Create(id, application, null, null, DraftPresentationKind.GenericText, 1, 1,
            new byte[] { (byte)sequence }, ProtectedDraftPayload.Create(1, new byte[] { (byte)sequence }), sequence,
            updated, updated, expiry ?? updated.AddMinutes(5), RecoverableState.Recoverable);
    }

    private static (string Directory, string Path) TempStore()
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DraftRescue-WP43-" + Guid.NewGuid().ToString("N"));
        return (directory, System.IO.Path.Combine(directory, "drafts.db"));
    }

    private static void DeleteTemp(string directory)
    {
        for (var attempt = 0; attempt < 5 && System.IO.Directory.Exists(directory); attempt++)
        {
            try
            {
                System.IO.Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(50);
            }
        }
    }
}
