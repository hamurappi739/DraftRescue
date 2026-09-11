using DraftRescue.Domain.Drafts;

namespace DraftRescue.Application.Models;

public sealed class CheckpointCandidate
{
    private readonly byte[] _matchMetadata;

    private CheckpointCandidate(
        DraftId draftId,
        ulong contextGeneration,
        ulong currentContextGeneration,
        FieldTextSnapshot snapshot,
        string applicationId,
        string? appProfileId,
        int? appProfileVersion,
        DraftPresentationKind presentationKind,
        int fingerprintVersion,
        int matchMetadataVersion,
        byte[] matchMetadata,
        TimeSpan retention)
    {
        DraftId = draftId;
        ContextGeneration = contextGeneration;
        CurrentContextGeneration = currentContextGeneration;
        Snapshot = snapshot;
        ApplicationId = applicationId;
        AppProfileId = appProfileId;
        AppProfileVersion = appProfileVersion;
        PresentationKind = presentationKind;
        FingerprintVersion = fingerprintVersion;
        MatchMetadataVersion = matchMetadataVersion;
        _matchMetadata = matchMetadata.ToArray();
        Retention = retention;
    }

    public DraftId DraftId { get; }
    public ulong ContextGeneration { get; }
    public ulong CurrentContextGeneration { get; }
    public FieldTextSnapshot Snapshot { get; }
    public string ApplicationId { get; }
    public string? AppProfileId { get; }
    public int? AppProfileVersion { get; }
    public DraftPresentationKind PresentationKind { get; }
    public int FingerprintVersion { get; }
    public int MatchMetadataVersion { get; }
    public ReadOnlyMemory<byte> MatchMetadataBytes => _matchMetadata.ToArray();
    public TimeSpan Retention { get; }

    public static CheckpointCandidate Create(
        DraftId draftId,
        ulong contextGeneration,
        ulong currentContextGeneration,
        FieldTextSnapshot snapshot,
        string applicationId,
        string? appProfileId,
        int? appProfileVersion,
        DraftPresentationKind presentationKind,
        int fingerprintVersion,
        int matchMetadataVersion,
        ReadOnlySpan<byte> matchMetadataBytes,
        TimeSpan retention)
    {
        if (draftId.Value == Guid.Empty) throw new ArgumentException("Draft id must be non-empty.", nameof(draftId));
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        if (appProfileVersion is <= 0) throw new ArgumentOutOfRangeException(nameof(appProfileVersion));
        if (fingerprintVersion <= 0) throw new ArgumentOutOfRangeException(nameof(fingerprintVersion));
        if (matchMetadataVersion <= 0) throw new ArgumentOutOfRangeException(nameof(matchMetadataVersion));
        if (matchMetadataBytes.IsEmpty) throw new ArgumentException("Match metadata must not be empty.", nameof(matchMetadataBytes));
        if (retention <= TimeSpan.Zero || retention > TimeSpan.FromDays(1)) throw new ArgumentOutOfRangeException(nameof(retention));
        return new CheckpointCandidate(draftId, contextGeneration, currentContextGeneration, snapshot, applicationId, appProfileId,
            appProfileVersion, presentationKind, fingerprintVersion, matchMetadataVersion, matchMetadataBytes.ToArray(), retention);
    }
}

public enum CheckpointOutcome
{
    Committed = 0,
    IdempotentNoChange = 1,
    StaleIgnored = 2,
    SkippedStaleGeneration = 3,
    SkippedEmpty = 4,
    Cancelled = 5,
    ProtectionFailed = 6,
    RepositoryFailed = 7
}

public readonly record struct CheckpointResult(CheckpointOutcome Outcome, ulong SnapshotSequence);
