using Domain.Common;

namespace Domain.Buses.Entities;

public sealed class Bus : Entity
{
    public string Plate { get; set; } = "";
    public string InternalCode { get; set; } = "";
    public string DriverName { get; set; } = "";
    public BusType BusType { get; set; } = BusType.Standard;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public OccupancyLevel OccupancyLevel { get; set; } = OccupancyLevel.Low;
    public Guid AssignedRouteId { get; set; }
    public string NextStation { get; set; } = "";
    public int EtaMinutes { get; set; }
    public BusStatus Status { get; set; } = BusStatus.InService;
}
