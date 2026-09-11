using DraftRescue.Application.Models;
using DraftRescue.Application.Security;

namespace DraftRescue.Application.Contracts.Security;

public interface ISecureInputGuard
{
    ValueTask<CaptureEligibility> EvaluateAsync(
        ObservedFieldContext context,
        CancellationToken cancellationToken);
}
