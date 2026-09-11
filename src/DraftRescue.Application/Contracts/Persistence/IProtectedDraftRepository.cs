using DraftRescue.Application.Models;
using DraftRescue.Domain.Drafts;

namespace DraftRescue.Application.Contracts.Persistence;

/// <summary>
/// Ciphertext-only repository boundary. No plaintext or field snapshot overloads are permitted.
/// </summary>
public interface IProtectedDraftRepository
{
    Task<ProtectedDraftUpsertResult> UpsertAsync(ProtectedDraftRecordV1 record, CancellationToken cancellationToken = default);

    Task<ProtectedDraftRecordV1?> GetProtectedAsync(DraftId draftId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecoverableDraftMetadata>> ListRecoverableMetadataAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(DraftId draftId, CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
