using Domain.Common;

namespace Domain.RouteStations.Entities;

public sealed class RouteStation : Entity
{
    public Guid RouteId { get; set; }
    public Guid StationId { get; set; }
    public int StopOrder { get; set; }
    public int EstimatedMinutesFromStart { get; set; }
    public bool IsTransferPoint { get; set; }
}

