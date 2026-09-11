using System.Reflection;
using System.Text.Json;
using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Models;
using DraftRescue.Domain.Drafts;
using DraftRescue.Infrastructure.Persistence;
using Xunit;

namespace DraftRescue.Tests.Persistence;

public sealed class Phase4ContractsTests
{
    [Fact]
    public void RepositoryBoundaryAcceptsOnlyProtectedRecordAndOpaqueIdentifiers()
    {
        var forbidden = new[] { typeof(string), typeof(DraftPlaintextPayload), typeof(byte[]), typeof(FieldTextSnapshot) };
        var parameters = typeof(IProtectedDraftRepository).GetMethods()
            .SelectMany(method => method.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.DoesNotContain(parameters, type => forbidden.Contains(type));
        Assert.Contains(parameters, type => type == typeof(ProtectedDraftRecordV1));
        Assert.DoesNotContain(typeof(IProtectedDraftRepository).GetMethods(), method =>
            method.Name.Contains("Decrypt", StringComparison.OrdinalIgnoreCase) ||
            method.Name.Contains("Plaintext", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProtectedRecordHasCanonicalShapeAndNoContentBearingMembers()
    {
        var names = typeof(ProtectedDraftRecordV1).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var forbidden = new[] { "Text", "Plaintext", "Preview", "Snippet", "RawUrl", "WindowTitle", "FieldName", "ClipboardText", "DraftPayloadV1" };

        Assert.DoesNotContain(forbidden, names.Contains);
        Assert.Equal(16, names.Count);
        Assert.Contains(nameof(ProtectedDraftRecordV1.RecordSchemaVersion), names);
        Assert.Contains(nameof(ProtectedDraftRecordV1.ProtectedPayloadBytes), names);
        Assert.Contains(nameof(ProtectedDraftRecordV1.SnapshotSequence), names);
    }

    [Fact]
    public void ProtectedPayloadCopiesBytesAtBothBoundaries()
    {
        var source = new byte[] { 1, 2, 3 };
        var payload = ProtectedDraftPayload.Create(1, source);
        source[0] = 9;
        var firstRead = payload.Bytes.ToArray();
        firstRead[1] = 8;

        Assert.Equal(new byte[] { 1, 2, 3 }, payload.Bytes.ToArray());
        Assert.NotSame(source, payload.Bytes.ToArray());
    }

    [Fact]
    public void TransientPlaintextPayloadDoesNotSerializeByDefault()
    {
        var payload = DraftPlaintextPayload.Create("phase4-contract-canary");
        Assert.DoesNotContain("phase4-contract-canary", JsonSerializer.Serialize(payload), StringComparison.Ordinal);
    }

    [Fact]
    public void ProtectedRecordCopiesMetadataAndPayloadAndPreservesVersions()
    {
        var metadata = new byte[] { 7, 8 };
        var record = ProtectedDraftRecordV1.Create(
            DraftId.New(), "notepad", "notepad.default", 1, DraftPresentationKind.Document,
            1, 1, metadata, ProtectedDraftPayload.Create(1, new byte[] { 4, 5 }), 42,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(1),
            DateTimeOffset.UnixEpoch.AddMinutes(1), RecoverableState.Recoverable);
        metadata[0] = 0;

        Assert.Equal(1, record.RecordSchemaVersion);
        Assert.Equal(1, record.ProtectionVersion);
        Assert.Equal(42, record.SnapshotSequence);
        Assert.Equal(new byte[] { 7, 8 }, record.MatchMetadataBytes.ToArray());
        Assert.Equal(new byte[] { 4, 5 }, record.ProtectedPayloadBytes.ToArray());
    }

    [Fact]
    public void ProtectedRecordRejectsInvalidTemporalOrSequenceState()
    {
        var id = DraftId.New();
        Assert.Throws<ArgumentOutOfRangeException>(() => ProtectedDraftRecordV1.Create(
            id, "app", null, null, DraftPresentationKind.Unknown, 1, 1, new byte[] { 1 },
            ProtectedDraftPayload.Create(1, new byte[] { 2 }), -1,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, RecoverableState.Recoverable));
        Assert.Throws<ArgumentException>(() => ProtectedDraftRecordV1.Create(
            id, "app", null, null, DraftPresentationKind.Unknown, 1, 1, new byte[] { 1 },
            ProtectedDraftPayload.Create(1, new byte[] { 2 }), 0,
            DateTimeOffset.UnixEpoch.AddDays(1), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, RecoverableState.Recoverable));
    }

    [Fact]
    public void SchemaMatchesCanonicalCiphertextOnlyTable()
    {
        var sql = SqliteSchemaV1.CreateDraftsTableSql;
        Assert.Contains("draft_id BLOB NOT NULL PRIMARY KEY", sql, StringComparison.Ordinal);
        Assert.Contains("protected_payload BLOB NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("WITHOUT ROWID", sql, StringComparison.Ordinal);
        Assert.Contains("CHECK(snapshot_sequence >= 0)", sql, StringComparison.Ordinal);
        Assert.Contains("CHECK(updated_at_utc_ms >= created_at_utc_ms)", sql, StringComparison.Ordinal);
        Assert.Contains("CHECK(expires_at_utc_ms >= updated_at_utc_ms)", sql, StringComparison.Ordinal);
        foreach (var forbidden in new[] { "plaintext", "preview", "snippet", "raw_url", "window_title", "revision", "keystroke" })
            Assert.DoesNotContain(forbidden, sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SchemaUsesSafeConnectionPragmasAndNoWal()
    {
        Assert.Equal(1, SqliteSchemaV1.UserVersion);
        Assert.Equal("DELETE", SqliteSchemaV1.JournalMode);
        Assert.Equal("ON", SqliteSchemaV1.SecureDelete);
        Assert.Equal("EXTRA", SqliteSchemaV1.Synchronous);
        Assert.Equal(1500, SqliteSchemaV1.BusyTimeoutMilliseconds);
        Assert.DoesNotContain(SqliteSchemaV1.RequiredPragmas, pragma => pragma.Contains("WAL", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(SqliteSchemaV1.RequiredPragmas, pragma => pragma.Contains("secure_delete=ON", StringComparison.Ordinal));
        Assert.Contains("ix_drafts_expires_at", SqliteSchemaV1.CreateExpiryIndexSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationAssemblyDoesNotReferenceInfrastructure()
    {
        var refs = typeof(IProtectedDraftRepository).Assembly.GetReferencedAssemblies().Select(value => value.Name);
        Assert.DoesNotContain("DraftRescue.Infrastructure", refs);
        Assert.DoesNotContain(refs, value => value is not null && value.StartsWith("Avalonia", StringComparison.Ordinal));
    }
}
