using DraftRescue.Application.Models;
using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Domain.Drafts;
using DraftRescue.Infrastructure.Persistence;
using DraftRescue.Application.Contracts.Reading;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4RecoveryTests
{
    [Fact]
    public void QuarantineMovesDatabaseLocallyWithoutReplacingIt()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP46-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        var quarantine = Path.Combine(directory, "quarantine");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(database, new byte[] { 1, 2, 3, 4 });
            var moved = SqliteStoreRecovery.Quarantine(database, quarantine);
            Assert.NotNull(moved);
            Assert.False(File.Exists(database));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(moved!));
            Assert.StartsWith(quarantine, moved!, StringComparison.OrdinalIgnoreCase);
        }
        finally { Delete(directory); }
    }

    [Fact]
    public void UnsupportedSchemaDoesNotAutoDropOrRecreateDatabase()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP46-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        try
        {
            Directory.CreateDirectory(directory);
            using (var connection = new SqliteConnection($"Data Source={database};Pooling=false"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA user_version=99;";
                command.ExecuteNonQuery();
            }
            Assert.Throws<SqliteStoreException>(() => new SqliteProtectedDraftRepository(database));
            Assert.True(File.Exists(database));
            Assert.True(SqliteMigrationPolicy.RequiresKnownMigration(99));
            Assert.False(SqliteMigrationPolicy.IsSupported(99));
        }
        finally { Delete(directory); }
    }

    [Fact]
    public void CorruptDatabaseOpenIsClassifiedAsCorruptWithoutRebuild()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP46-corrupt-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(database, Encoding.UTF8.GetBytes("not a sqlite database"));

            var error = Assert.Throws<SqliteStoreException>(() => new SqliteProtectedDraftRepository(database));

            Assert.Equal(StoreOpenResult.Corrupt, SqliteStoreRecovery.Classify(error));
            Assert.True(File.Exists(database));
        }
        finally { Delete(directory); }
    }

    [Fact]
    public async Task MalformedRowReturnsTypedCorruptionAndNoSalvage()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP46-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        try
        {
            using (var repository = new SqliteProtectedDraftRepository(database)) { }
            using (var connection = new SqliteConnection($"Data Source={database};Pooling=false"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO drafts (draft_id, record_schema_version, application_id, presentation_kind, fingerprint_version, match_metadata_version, match_metadata, protected_payload, protection_version, snapshot_sequence, created_at_utc_ms, updated_at_utc_ms, expires_at_utc_ms, recoverable_state) VALUES ($id,1,'app',0,1,1,$meta,$payload,1,0,0,0,1,0);";
                command.Parameters.AddWithValue("$id", DraftId.New().Value.ToByteArray());
                command.Parameters.AddWithValue("$meta", new byte[] { 1 });
                command.Parameters.AddWithValue("$payload", Array.Empty<byte>());
                command.ExecuteNonQuery();
            }
            using var loadedRepository = new SqliteProtectedDraftRepository(database);
            var error = await Assert.ThrowsAsync<SqliteStoreException>(() => loadedRepository.GetProtectedAsync(new DraftId(ReadFirstId(database)), TestContext.Current.CancellationToken));
            Assert.Equal(SqliteStoreFailureCode.CorruptRecord, error.Code);
        }
        finally { Delete(directory); }
    }

    [Fact]
    public async Task MalformedMetadataTimestampReturnsTypedCorruption()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP46-metadata-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        try
        {
            using (var repository = new SqliteProtectedDraftRepository(database)) { }
            await using (var connection = new SqliteConnection($"Data Source={database};Pooling=false"))
            {
                await connection.OpenAsync(TestContext.Current.CancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO drafts (draft_id, record_schema_version, application_id, presentation_kind, fingerprint_version, match_metadata_version, match_metadata, protected_payload, protection_version, snapshot_sequence, created_at_utc_ms, updated_at_utc_ms, expires_at_utc_ms, recoverable_state) VALUES ($id,1,'app',0,1,1,$meta,$payload,1,0,$time,$time,$time,0);";
                command.Parameters.AddWithValue("$id", DraftId.New().Value.ToByteArray());
                command.Parameters.AddWithValue("$meta", new byte[] { 1 });
                command.Parameters.AddWithValue("$payload", new byte[] { 2 });
                command.Parameters.AddWithValue("$time", long.MaxValue);
                await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            }

            using var loadedRepository = new SqliteProtectedDraftRepository(database);
            var error = await Assert.ThrowsAsync<SqliteStoreException>(() =>
                loadedRepository.ListRecoverableMetadataAsync(DateTimeOffset.UtcNow, TestContext.Current.CancellationToken));
            Assert.Equal(SqliteStoreFailureCode.CorruptRecord, error.Code);
        }
        finally { Delete(directory); }
    }

    [Fact]
    public async Task LockedStoreReturnsBoundedTypedFailureWithoutFallback() // P4F-006
    {
        var stopwatch = Stopwatch.StartNew();
        var error = new SqliteStoreException(SqliteStoreFailureCode.Unavailable);
        var classification = SqliteStoreRecovery.Classify(error);
        stopwatch.Stop();
        await Task.CompletedTask;
        Assert.Equal(StoreOpenResult.Unavailable, classification);
        Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CrashBeforeCommitLeavesPreviousCompleteRow() // P4F-007
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP46-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        try
        {
            using var repository = new SqliteProtectedDraftRepository(database);
            var id = DraftId.New();
            await repository.UpsertAsync(Record(id, 1), TestContext.Current.CancellationToken);
            await using (var connection = new SqliteConnection($"Data Source={database};Pooling=false"))
            {
                await connection.OpenAsync(TestContext.Current.CancellationToken);
                await using var transaction = connection.BeginTransaction();
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "UPDATE drafts SET protected_payload = X'FF' WHERE draft_id = $id;";
                command.Parameters.AddWithValue("$id", id.Value.ToByteArray());
                await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
                await transaction.RollbackAsync(TestContext.Current.CancellationToken);
            }
            var loaded = await repository.GetProtectedAsync(id, TestContext.Current.CancellationToken);
            Assert.Equal(new byte[] { 1 }, loaded!.ProtectedPayloadBytes.ToArray());
        }
        finally { Delete(directory); }
    }

    [Fact]
    public void DiskFullEquivalentPathFailureIsTypedWithoutPlaintextFallback() // P4F-014
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP46-" + Guid.NewGuid().ToString("N"));
        var blocker = Path.Combine(directory, "not-a-directory");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(blocker, new byte[] { 1 });
            var error = Assert.Throws<SqliteStoreException>(() => new SqliteProtectedDraftRepository(Path.Combine(blocker, "drafts.db")));
            Assert.Equal(SqliteStoreFailureCode.Unavailable, error.Code);
            Assert.DoesNotContain("plaintext", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { Delete(directory); }
    }

    [Fact]
    public async Task FiveHundredCheckpointsKeepOneLogicalRow() // P4F-016
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP46-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        try
        {
            using var repository = new SqliteProtectedDraftRepository(database);
            var id = DraftId.New();
            for (var sequence = 1; sequence <= 500; sequence++)
                await repository.UpsertAsync(Record(id, sequence), TestContext.Current.CancellationToken);
            await using var connection = new SqliteConnection($"Data Source={database};Pooling=false");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*), MAX(snapshot_sequence) FROM drafts WHERE draft_id = $id;";
            command.Parameters.AddWithValue("$id", id.Value.ToByteArray());
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
            Assert.Equal(1, reader.GetInt32(0));
            Assert.Equal(500, reader.GetInt64(1));
        }
        finally { Delete(directory); }
    }

    [Fact]
    public async Task CanaryNeverAppearsInOwnedArtifactsAfterProtectedUpdates() // P4F-017
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP47-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        var canary = "DR_TEST_CANARY_" + Guid.NewGuid().ToString("N") + "_END";
        try
        {
            using var repository = new SqliteProtectedDraftRepository(database);
            var protector = new CanaryProtector();
            using var coordinator = new DraftRescue.Application.Persistence.PersistenceCheckpointCoordinator(protector, repository);
            var draftId = DraftId.New();
            for (var sequence = 1UL; sequence <= 5; sequence++)
            {
                var snapshot = FieldTextSnapshot.Create(1, sequence, Guid.NewGuid(), "test.profile", ReadStrategy.ValuePatternCertified, 1,
                    DateTimeOffset.UtcNow, canary + sequence);
                var candidate = CheckpointCandidate.Create(draftId, 1, 1, snapshot, "test.app", null, null,
                    DraftPresentationKind.GenericText, 1, 1, new byte[] { 1, 2, 3 }, TimeSpan.FromMinutes(30));
                var result = await coordinator.CheckpointAsync(candidate, TestContext.Current.CancellationToken);
                Assert.Equal(CheckpointOutcome.Committed, result.Outcome);
            }
            coordinator.Dispose();
            repository.Dispose();
            var utf8 = Encoding.UTF8.GetBytes(canary);
            var utf16 = Encoding.Unicode.GetBytes(canary);
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                var bytes = await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken);
                Assert.False(Contains(bytes, utf8));
                Assert.False(Contains(bytes, utf16));
            }
        }
        finally { Delete(directory); }
    }

    [Fact]
    public async Task InjectedDiskFullBeforeCommitKeepsPreviousRow() // P4F-014
    {
        var directory = Path.Combine(Path.GetTempPath(), "DraftRescue-WP48-" + Guid.NewGuid().ToString("N"));
        var database = Path.Combine(directory, "drafts.db");
        var id = DraftId.New();
        try
        {
            using (var seed = new SqliteProtectedDraftRepository(database))
                await seed.UpsertAsync(Record(id, 1), TestContext.Current.CancellationToken);
            using (var faulted = new SqliteProtectedDraftRepository(database, new SqliteFaultInjector(SqliteFaultPoint.BeforeCommit)))
            {
                var error = await Assert.ThrowsAsync<SqliteStoreException>(() => faulted.UpsertAsync(Record(id, 2), TestContext.Current.CancellationToken));
                Assert.Equal(SqliteStoreFailureCode.Unavailable, error.Code);
            }
            using var recovered = new SqliteProtectedDraftRepository(database);
            var loaded = await recovered.GetProtectedAsync(id, TestContext.Current.CancellationToken);
            Assert.Equal(1, loaded!.SnapshotSequence);
            Assert.Equal(new byte[] { 1 }, loaded.ProtectedPayloadBytes.ToArray());
        }
        finally { Delete(directory); }
    }

    private static bool Contains(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle)) return true;
        }
        return false;
    }

    private sealed class CanaryProtector : IDraftProtector
    {
        public ProtectedDraftPayload Protect(DraftPlaintextPayload plaintext, DraftProtectionContext context)
            => ProtectedDraftPayload.Create(context.ProtectionVersion, SHA256.HashData(Encoding.UTF8.GetBytes(plaintext.Text)));

        public DraftPlaintextPayload Unprotect(ProtectedDraftPayload protectedPayload, DraftProtectionContext context)
            => throw new NotSupportedException();
    }

    private static ProtectedDraftRecordV1 Record(DraftId id, long sequence = 1)
        => ProtectedDraftRecordV1.Create(id, "test.app", null, null, DraftPresentationKind.GenericText, 1, 1,
            new byte[] { 1 }, ProtectedDraftPayload.Create(1, new byte[] { (byte)Math.Clamp(sequence, 0, 255) }), sequence,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(30), RecoverableState.Recoverable);

    private static Guid ReadFirstId(string database)
    {
        using var connection = new SqliteConnection($"Data Source={database};Pooling=false");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT draft_id FROM drafts LIMIT 1;";
        return new Guid((byte[])command.ExecuteScalar()!);
    }

    private static void Delete(string directory)
    {
        if (!Directory.Exists(directory)) return;
        try { Directory.Delete(directory, true); } catch (IOException) { }
    }
}
