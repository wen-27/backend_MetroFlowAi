using Domain.Common;

namespace Domain.BusPositions.Entities;

public sealed class BusPosition : Entity
{
    public Guid BusId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal SpeedKmh { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public Guid? CurrentStationId { get; set; }
    public Guid? NextStationId { get; set; }
}

