using Domain.Common;

namespace Domain.RouteSegments.Entities;

public sealed class RouteSegment : Entity
{
    public Guid RouteId { get; set; }
    public Guid OriginStationId { get; set; }
    public Guid DestinationStationId { get; set; }
    public decimal DistanceKm { get; set; }
    public int BaseTravelTimeMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

