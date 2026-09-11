using DraftRescue.Application.Models;

namespace DraftRescue.Application.Contracts.Observation;

/// <summary>
/// Observes foreground application changes without reading target content.
/// </summary>
public interface IForegroundContextSource : IAsyncDisposable
{
    IAsyncEnumerable<ForegroundApplicationContextChanged> WatchAsync(
        CancellationToken cancellationToken);
}
