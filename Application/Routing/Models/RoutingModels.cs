using Domain.Common;

namespace Application.Routing.Models;

public sealed record GraphNode(Guid StationId, string Name);
public sealed record GraphEdge(Guid RouteId, Guid OriginStationId, Guid DestinationStationId, int WeightMinutes, int BaseMinutes, string Reason);

public sealed record RoutePlanStep(int Order, Guid StationId, string StationName, string Instruction);

public sealed record RoutePlanResult(
    Guid OriginStationId,
    Guid DestinationStationId,
    string Origin,
    string Destination,
    string RecommendedRoute,
    int EstimatedMinutes,
    OccupancyLevel OccupancyLevel,
    bool HasIncidents,
    string Summary,
    IReadOnlyList<RoutePlanStep> Steps,
    IReadOnlyList<string> Alerts);

public sealed record RoutePlanRequest(Guid OriginStationId, Guid DestinationStationId, bool AvoidHighCongestion = true, bool AvoidIncidents = true);

