using Application.Routing.Models;

namespace Application.Routing.Abstractions;

public interface IRouteGraphService
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<GraphEdge>>> BuildGraphAsync(bool forceRebuild = false, CancellationToken cancellationToken = default);
    void Clear();
}

