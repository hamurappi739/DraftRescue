using DraftRescue.Application.Models;

namespace DraftRescue.Application.Contracts.Observation;

/// <summary>
/// Future boundary for supported-field observation. This is intentionally not
/// a global keystroke-history abstraction.
/// </summary>
public interface IInputObservationSource
{
    IAsyncEnumerable<ObservedFieldContext> ObserveAsync(CancellationToken cancellationToken);
}
