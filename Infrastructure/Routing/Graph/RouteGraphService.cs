using Application.Common;
using Application.Routing.Abstractions;
using Application.Routing.Models;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Routing.Graph;

public sealed class RouteGraphService(IMetroFlowDbContext db, IMemoryCache cache) : IRouteGraphService
{
    private const string CacheKey = "metroflow-route-graph";

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<GraphEdge>>> BuildGraphAsync(bool forceRebuild = false, CancellationToken cancellationToken = default)
    {
        if (!forceRebuild && cache.TryGetValue(CacheKey, out IReadOnlyDictionary<Guid, IReadOnlyList<GraphEdge>>? cached) && cached is not null)
            return cached;

        var stations = await db.Stations.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var incidents = await db.Incidents.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken);
        var buses = await db.Buses.AsNoTracking().ToListAsync(cancellationToken);
        var segments = await db.RouteSegments.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken);
        var edges = new Dictionary<Guid, List<GraphEdge>>();

        foreach (var segment in segments)
        {
            var origin = stations[segment.OriginStationId];
            var destination = stations[segment.DestinationStationId];
            var routeBuses = buses.Where(x => x.AssignedRouteId == segment.RouteId).ToList();
            var occupancyPenalty = routeBuses.Any(x => x.OccupancyLevel == OccupancyLevel.High) ? 4 :
                routeBuses.Any(x => x.OccupancyLevel == OccupancyLevel.Medium) ? 2 : 0;
            var incidentPenalty = incidents.Any(x => x.RouteId == segment.RouteId || x.StationId == origin.Id || x.StationId == destination.Id)
                ? 10 : 0;
            var delayPenalty = routeBuses.Any(x => x.Status == BusStatus.Delayed) ? 3 : 0;
            var congestionPenalty = destination.CurrentOccupancyLevel == OccupancyLevel.High ? 5 :
                destination.CurrentOccupancyLevel == OccupancyLevel.Medium ? 2 : 0;
            var weight = segment.BaseTravelTimeMinutes + occupancyPenalty + incidentPenalty + delayPenalty + congestionPenalty;
            var reason = $"base:{segment.BaseTravelTimeMinutes}; ocupacion:{occupancyPenalty}; incidente:{incidentPenalty}; retraso:{delayPenalty}; congestion:{congestionPenalty}";

            Add(edges, segment.OriginStationId, new GraphEdge(segment.RouteId, segment.OriginStationId, segment.DestinationStationId, weight, segment.BaseTravelTimeMinutes, reason));
            Add(edges, segment.DestinationStationId, new GraphEdge(segment.RouteId, segment.DestinationStationId, segment.OriginStationId, weight, segment.BaseTravelTimeMinutes, reason));
        }

        var graph = edges.ToDictionary(x => x.Key, x => (IReadOnlyList<GraphEdge>)x.Value);
        cache.Set(CacheKey, graph, TimeSpan.FromMinutes(5));
        return graph;
    }

    public void Clear() => cache.Remove(CacheKey);

    private static void Add(Dictionary<Guid, List<GraphEdge>> edges, Guid stationId, GraphEdge edge)
    {
        if (!edges.TryGetValue(stationId, out var list))
        {
            list = [];
            edges[stationId] = list;
        }
        list.Add(edge);
    }
}

