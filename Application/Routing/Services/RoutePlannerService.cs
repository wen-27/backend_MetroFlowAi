using Application.Common;
using Application.Routing.Abstractions;
using Application.Routing.Models;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Routing.Services;

public sealed class RoutePlannerService(IRouteGraphService graphService, IMetroFlowDbContext db) : IRoutePlannerService
{
    public async Task<RoutePlanResult> PlanAsync(RoutePlanRequest request, CancellationToken cancellationToken = default)
    {
        var stations = await db.Stations.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        if (!stations.ContainsKey(request.OriginStationId) || !stations.ContainsKey(request.DestinationStationId))
            throw new InvalidOperationException("Origen o destino no existen.");

        var graph = await graphService.BuildGraphAsync(cancellationToken: cancellationToken);
        var distances = stations.Keys.ToDictionary(x => x, _ => int.MaxValue);
        var previous = new Dictionary<Guid, (Guid StationId, GraphEdge Edge)>();
        var pending = new HashSet<Guid>(stations.Keys);
        distances[request.OriginStationId] = 0;

        while (pending.Count > 0)
        {
            var current = pending.OrderBy(x => distances[x]).First();
            pending.Remove(current);
            if (current == request.DestinationStationId || distances[current] == int.MaxValue) break;
            if (!graph.TryGetValue(current, out var edges)) continue;

            foreach (var edge in edges)
            {
                var penalty = 0;
                if (!request.AvoidIncidents && edge.Reason.Contains("incidente", StringComparison.OrdinalIgnoreCase)) penalty -= 5;
                if (!request.AvoidHighCongestion && edge.Reason.Contains("ocupacion", StringComparison.OrdinalIgnoreCase)) penalty -= 2;
                var candidate = distances[current] + Math.Max(1, edge.WeightMinutes + penalty);
                if (candidate >= distances[edge.DestinationStationId]) continue;
                distances[edge.DestinationStationId] = candidate;
                previous[edge.DestinationStationId] = (current, edge);
            }
        }

        if (!previous.ContainsKey(request.DestinationStationId))
            return EmptyPlan(request, stations[request.OriginStationId].Name, stations[request.DestinationStationId].Name);

        var path = new List<Guid> { request.DestinationStationId };
        var routeIds = new List<Guid>();
        var cursor = request.DestinationStationId;
        while (cursor != request.OriginStationId)
        {
            var step = previous[cursor];
            routeIds.Add(step.Edge.RouteId);
            cursor = step.StationId;
            path.Add(cursor);
        }
        path.Reverse();
        routeIds.Reverse();

        var route = await db.Routes.AsNoTracking()
            .Where(x => routeIds.Contains(x.Id))
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .FirstOrDefaultAsync(cancellationToken);

        var routeAlerts = await db.Alerts.AsNoTracking()
            .Where(x => x.IsActive && (x.RouteId == route!.Id || path.Contains(x.StationId ?? Guid.Empty)))
            .Select(x => x.Title)
            .ToListAsync(cancellationToken);

        var hasIncidents = await db.Incidents.AsNoTracking()
            .AnyAsync(x => x.IsActive && ((x.RouteId != null && routeIds.Contains(x.RouteId.Value)) || (x.StationId != null && path.Contains(x.StationId.Value))), cancellationToken);

        var steps = path.Select((id, index) =>
        {
            var name = stations[id].Name;
            var instruction = index == 0 ? $"Inicia en estacion {name}" :
                index == path.Count - 1 ? $"Llega a {name}" : $"Continua hacia {name}";
            return new RoutePlanStep(index + 1, id, name, instruction);
        }).ToList();

        var occupancy = path.Select(x => stations[x].CurrentOccupancyLevel).DefaultIfEmpty(OccupancyLevel.Low).Max();
        var summary = hasIncidents
            ? "La ruta evita o penaliza segmentos con incidentes y congestion activa."
            : "Ruta recomendada calculada con tiempos base, ocupacion y congestion actual.";

        return new RoutePlanResult(
            request.OriginStationId,
            request.DestinationStationId,
            stations[request.OriginStationId].Name,
            stations[request.DestinationStationId].Name,
            route?.Code ?? "Combinada",
            distances[request.DestinationStationId],
            occupancy,
            hasIncidents,
            summary,
            steps,
            routeAlerts);
    }

    private static RoutePlanResult EmptyPlan(RoutePlanRequest request, string origin, string destination) =>
        new(request.OriginStationId, request.DestinationStationId, origin, destination, "N/A", 0, OccupancyLevel.Low, false,
            "No hay conexion disponible entre las estaciones seleccionadas.", [], []);
}

