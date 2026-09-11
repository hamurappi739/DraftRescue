using System.Text.Json.Serialization;
using DraftRescue.Application.Contracts.Reading;

namespace DraftRescue.Application.Models;

/// <summary>
/// One complete, transient current text observation. It intentionally exposes
/// no platform handle or target identity and never prints its text.
/// </summary>
public sealed class FieldTextSnapshot
{
    private FieldTextSnapshot(
        ulong contextGeneration,
        ulong snapshotSequence,
        Guid captureAttemptId,
        string profileId,
        ReadStrategy readStrategy,
        long capturedAtMonotonic,
        DateTimeOffset capturedAtUtc,
        string text)
    {
        ContextGeneration = contextGeneration;
        SnapshotSequence = snapshotSequence;
        CaptureAttemptId = captureAttemptId;
        ProfileId = profileId;
        ReadStrategy = readStrategy;
        CapturedAtMonotonic = capturedAtMonotonic;
        CapturedAtUtc = capturedAtUtc;
        Text = text;
    }

    [JsonIgnore]
    public ulong ContextGeneration { get; }

    [JsonIgnore]
    public ulong SnapshotSequence { get; }

    [JsonIgnore]
    public Guid CaptureAttemptId { get; }

    [JsonIgnore]
    public string ProfileId { get; }

    [JsonIgnore]
    public ReadStrategy ReadStrategy { get; }

    [JsonIgnore]
    public long CapturedAtMonotonic { get; }

    [JsonIgnore]
    public DateTimeOffset CapturedAtUtc { get; }

    [JsonIgnore]
    public string Text { get; }

    [JsonIgnore]
    public int TextLengthUtf16 => Text.Length;

    [JsonIgnore]
    public bool IsEmpty => Text.Length == 0;

    public static FieldTextSnapshot Create(
        ulong contextGeneration,
        ulong snapshotSequence,
        Guid captureAttemptId,
        string profileId,
        ReadStrategy readStrategy,
        long capturedAtMonotonic,
        DateTimeOffset capturedAtUtc,
        string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(text);
        if (captureAttemptId == Guid.Empty)
        {
            throw new ArgumentException("Capture attempt id must be non-empty.", nameof(captureAttemptId));
        }

        return new FieldTextSnapshot(
            contextGeneration,
            snapshotSequence,
            captureAttemptId,
            profileId,
            readStrategy,
            capturedAtMonotonic,
            capturedAtUtc,
            text);
    }

    public override string ToString() => nameof(FieldTextSnapshot);
}
