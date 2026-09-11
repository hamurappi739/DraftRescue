using DraftRescue.Application.Models;

namespace DraftRescue.Application.Contracts.Observation;

/// <summary>
/// Composes content-free foreground and focused-element metadata observations.
/// </summary>
public interface IObservationCoordinator : IAsyncDisposable
{
    IAsyncEnumerable<ObservedContextChanged> WatchAsync(
        CancellationToken cancellationToken);
}
