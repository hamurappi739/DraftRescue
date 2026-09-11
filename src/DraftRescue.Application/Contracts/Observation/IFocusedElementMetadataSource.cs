using DraftRescue.Application.Models;

namespace DraftRescue.Application.Contracts.Observation;

/// <summary>
/// Observes focused UI Automation element metadata without reading target content.
/// </summary>
public interface IFocusedElementMetadataSource : IAsyncDisposable
{
    IAsyncEnumerable<FocusedElementMetadataChanged> WatchAsync(
        CancellationToken cancellationToken);
}
