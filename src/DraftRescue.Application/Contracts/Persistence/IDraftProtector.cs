using DraftRescue.Application.Models;

namespace DraftRescue.Application.Contracts.Persistence;

/// <summary>Protects transient payloads before they cross into persistence.</summary>
public interface IDraftProtector
{
    ProtectedDraftPayload Protect(DraftPlaintextPayload plaintext, DraftProtectionContext context);

    DraftPlaintextPayload Unprotect(ProtectedDraftPayload protectedPayload, DraftProtectionContext context);
}
