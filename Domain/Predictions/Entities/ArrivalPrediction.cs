using Domain.Common;

namespace Domain.Predictions.Entities;

public sealed class ArrivalPrediction : Entity
{
    public Guid RouteId { get; set; }
    public Guid StationId { get; set; }
    public Guid? BusId { get; set; }
    public int EstimatedArrivalMinutes { get; set; }
    public OccupancyLevel OccupancyLevel { get; set; } = OccupancyLevel.Low;
    public decimal Confidence { get; set; } = 0.85m;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

