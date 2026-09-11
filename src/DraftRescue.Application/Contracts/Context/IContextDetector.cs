using DraftRescue.Application.Models;

namespace DraftRescue.Application.Contracts.Context;

public interface IContextDetector
{
    ValueTask<ObservedFieldContext?> DetectCurrentFieldAsync(CancellationToken cancellationToken);
}
