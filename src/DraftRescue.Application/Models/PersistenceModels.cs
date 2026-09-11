using System.Text.Json.Serialization;
using DraftRescue.Domain.Drafts;

namespace DraftRescue.Application.Models;

/// <summary>Coarse, privacy-safe presentation category. It never contains user content.</summary>
public enum DraftPresentationKind
{
    Unknown = 0,
    GenericText = 1,
    MessageComposer = 2,
    WebForm = 3,
    Comment = 4,
    Document = 5,
    Note = 6
}

/// <summary>Lifecycle state used by the metadata-only recovery list.</summary>
public enum RecoverableState
{
    Recoverable = 0,
    Expired = 1,
    Unavailable = 2
}

/// <summary>Transient plaintext payload. It is accepted only by the protector boundary.</summary>
public sealed class DraftPlaintextPayload
{
    private DraftPlaintextPayload(string text) => Text = text;

    [JsonIgnore]
    public string Text { get; }

    public static DraftPlaintextPayload Create(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new DraftPlaintextPayload(text);
    }

    public override string ToString() => nameof(DraftPlaintextPayload);
}

/// <summary>Opaque protected payload. The byte array is copied on ingress and egress.</summary>
public sealed class ProtectedDraftPayload
{
    private readonly byte[] _bytes;

    private ProtectedDraftPayload(int protectionVersion, byte[] bytes)
    {
        ProtectionVersion = protectionVersion;
        _bytes = bytes.ToArray();
    }

    public int ProtectionVersion { get; }

    public ReadOnlyMemory<byte> Bytes => _bytes.ToArray();

    public static ProtectedDraftPayload Create(int protectionVersion, ReadOnlySpan<byte> bytes)
    {
        if (protectionVersion <= 0) throw new ArgumentOutOfRangeException(nameof(protectionVersion));
        if (bytes.IsEmpty) throw new ArgumentException("Protected payload must not be empty.", nameof(bytes));
        return new ProtectedDraftPayload(protectionVersion, bytes.ToArray());
    }

    public override string ToString() => nameof(ProtectedDraftPayload);
}

/// <summary>Context that binds protection to the logical draft and its format.</summary>
public readonly record struct DraftProtectionContext(DraftId DraftId, int ProtectionVersion)
{
    public static DraftProtectionContext Create(DraftId draftId, int protectionVersion)
    {
        if (draftId.Value == Guid.Empty) throw new ArgumentException("Draft id must be non-empty.", nameof(draftId));
        if (protectionVersion <= 0) throw new ArgumentOutOfRangeException(nameof(protectionVersion));
        return new DraftProtectionContext(draftId, protectionVersion);
    }
}

/// <summary>Only record shape accepted by the protected repository.</summary>
public sealed class ProtectedDraftRecordV1
{
    private readonly byte[] _matchMetadataBytes;
    private readonly byte[] _protectedPayloadBytes;

    private ProtectedDraftRecordV1(
        DraftId draftId,
        string applicationId,
        string? appProfileId,
        int? appProfileVersion,
        DraftPresentationKind presentationKind,
        int fingerprintVersion,
        int matchMetadataVersion,
        byte[] matchMetadataBytes,
        byte[] protectedPayloadBytes,
        int protectionVersion,
        long snapshotSequence,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        RecoverableState recoverableState)
    {
        DraftId = draftId;
        ApplicationId = applicationId;
        AppProfileId = appProfileId;
        AppProfileVersion = appProfileVersion;
        PresentationKind = presentationKind;
        FingerprintVersion = fingerprintVersion;
        MatchMetadataVersion = matchMetadataVersion;
        _matchMetadataBytes = matchMetadataBytes.ToArray();
        _protectedPayloadBytes = protectedPayloadBytes.ToArray();
        ProtectionVersion = protectionVersion;
        SnapshotSequence = snapshotSequence;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RecoverableState = recoverableState;
    }

    public DraftId DraftId { get; }
    public int RecordSchemaVersion => 1;
    public string ApplicationId { get; }
    public string? AppProfileId { get; }
    public int? AppProfileVersion { get; }
    public DraftPresentationKind PresentationKind { get; }
    public int FingerprintVersion { get; }
    public int MatchMetadataVersion { get; }
    public ReadOnlyMemory<byte> MatchMetadataBytes => _matchMetadataBytes.ToArray();
    public ReadOnlyMemory<byte> ProtectedPayloadBytes => _protectedPayloadBytes.ToArray();
    public int ProtectionVersion { get; }
    public long SnapshotSequence { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public RecoverableState RecoverableState { get; }

    public static ProtectedDraftRecordV1 Create(
        DraftId draftId,
        string applicationId,
        string? appProfileId,
        int? appProfileVersion,
        DraftPresentationKind presentationKind,
        int fingerprintVersion,
        int matchMetadataVersion,
        ReadOnlySpan<byte> matchMetadataBytes,
        ProtectedDraftPayload protectedPayload,
        long snapshotSequence,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        RecoverableState recoverableState)
    {
        if (draftId.Value == Guid.Empty) throw new ArgumentException("Draft id must be non-empty.", nameof(draftId));
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        if (appProfileVersion is <= 0) throw new ArgumentOutOfRangeException(nameof(appProfileVersion));
        if (fingerprintVersion <= 0) throw new ArgumentOutOfRangeException(nameof(fingerprintVersion));
        if (matchMetadataVersion <= 0) throw new ArgumentOutOfRangeException(nameof(matchMetadataVersion));
        if (matchMetadataBytes.IsEmpty) throw new ArgumentException("Match metadata must not be empty.", nameof(matchMetadataBytes));
        ArgumentNullException.ThrowIfNull(protectedPayload);
        if (snapshotSequence < 0) throw new ArgumentOutOfRangeException(nameof(snapshotSequence));
        if (updatedAtUtc < createdAtUtc) throw new ArgumentException("Updated time cannot precede creation time.", nameof(updatedAtUtc));
        if (expiresAtUtc < updatedAtUtc) throw new ArgumentException("Expiry cannot precede updated time.", nameof(expiresAtUtc));

        return new ProtectedDraftRecordV1(
            draftId, applicationId, appProfileId, appProfileVersion, presentationKind,
            fingerprintVersion, matchMetadataVersion, matchMetadataBytes.ToArray(),
            protectedPayload.Bytes.ToArray(), protectedPayload.ProtectionVersion,
            snapshotSequence, createdAtUtc, updatedAtUtc, expiresAtUtc, recoverableState);
    }

    public override string ToString() => nameof(ProtectedDraftRecordV1);
}

public sealed record RecoverableDraftMetadata(
    DraftId DraftId,
    string ApplicationId,
    DraftPresentationKind PresentationKind,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    RecoverableState RecoverableState);

public enum ProtectedDraftUpsertResult
{
    Inserted = 0,
    Updated = 1,
    IdempotentNoChange = 2,
    StaleIgnored = 3
}
