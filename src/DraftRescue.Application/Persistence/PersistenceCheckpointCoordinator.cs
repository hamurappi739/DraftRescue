using DraftRescue.Application.Contracts.Persistence;
using DraftRescue.Application.Models;

namespace DraftRescue.Application.Persistence;

/// <summary>Protect-before-repository checkpoint state machine with no retained plaintext queue.</summary>
public sealed class PersistenceCheckpointCoordinator : IDisposable
{
    private readonly IDraftProtector _protector;
    private readonly IProtectedDraftRepository _repository;
    private readonly SemaphoreSlim _checkpointGate = new(1, 1);
    private bool _disposed;

    public PersistenceCheckpointCoordinator(IDraftProtector protector, IProtectedDraftRepository repository)
    {
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<CheckpointResult> CheckpointAsync(CheckpointCandidate candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (candidate.ContextGeneration != candidate.CurrentContextGeneration)
            return new CheckpointResult(CheckpointOutcome.SkippedStaleGeneration, candidate.Snapshot.SnapshotSequence);
        if (candidate.Snapshot.IsEmpty)
            return new CheckpointResult(CheckpointOutcome.SkippedEmpty, candidate.Snapshot.SnapshotSequence);

        try { await _checkpointGate.WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { return new CheckpointResult(CheckpointOutcome.Cancelled, candidate.Snapshot.SnapshotSequence); }

        try
        {
            if (cancellationToken.IsCancellationRequested)
                return new CheckpointResult(CheckpointOutcome.Cancelled, candidate.Snapshot.SnapshotSequence);

            ProtectedDraftPayload protectedPayload;
            try
            {
                protectedPayload = _protector.Protect(
                    DraftPlaintextPayload.Create(candidate.Snapshot.Text),
                    DraftProtectionContext.Create(candidate.DraftId, 1));
            }
            catch (OperationCanceledException) { return new CheckpointResult(CheckpointOutcome.Cancelled, candidate.Snapshot.SnapshotSequence); }
            catch { return new CheckpointResult(CheckpointOutcome.ProtectionFailed, candidate.Snapshot.SnapshotSequence); }

            // Protection is synchronous and cannot observe the caller token. If cancellation
            // arrives while DPAPI is running, discard the ciphertext before constructing or
            // persisting a protected record.
            if (cancellationToken.IsCancellationRequested)
                return new CheckpointResult(CheckpointOutcome.Cancelled, candidate.Snapshot.SnapshotSequence);

            var updated = candidate.Snapshot.CapturedAtUtc;
            ProtectedDraftRecordV1 record;
            try
            {
                record = ProtectedDraftRecordV1.Create(candidate.DraftId, candidate.ApplicationId, candidate.AppProfileId,
                    candidate.AppProfileVersion, candidate.PresentationKind, candidate.FingerprintVersion,
                    candidate.MatchMetadataVersion, candidate.MatchMetadataBytes.Span, protectedPayload,
                    checked((long)candidate.Snapshot.SnapshotSequence), updated, updated, updated + candidate.Retention,
                    RecoverableState.Recoverable);
            }
            catch { return new CheckpointResult(CheckpointOutcome.ProtectionFailed, candidate.Snapshot.SnapshotSequence); }

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return new CheckpointResult(CheckpointOutcome.Cancelled, candidate.Snapshot.SnapshotSequence);

                var result = await _repository.UpsertAsync(record, cancellationToken).ConfigureAwait(false);
                return result switch
                {
                    ProtectedDraftUpsertResult.Inserted or ProtectedDraftUpsertResult.Updated => new CheckpointResult(CheckpointOutcome.Committed, candidate.Snapshot.SnapshotSequence),
                    ProtectedDraftUpsertResult.IdempotentNoChange => new CheckpointResult(CheckpointOutcome.IdempotentNoChange, candidate.Snapshot.SnapshotSequence),
                    _ => new CheckpointResult(CheckpointOutcome.StaleIgnored, candidate.Snapshot.SnapshotSequence)
                };
            }
            catch (OperationCanceledException) { return new CheckpointResult(CheckpointOutcome.Cancelled, candidate.Snapshot.SnapshotSequence); }
            catch { return new CheckpointResult(CheckpointOutcome.RepositoryFailed, candidate.Snapshot.SnapshotSequence); }
        }
        finally { _checkpointGate.Release(); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _checkpointGate.Dispose();
    }
}
