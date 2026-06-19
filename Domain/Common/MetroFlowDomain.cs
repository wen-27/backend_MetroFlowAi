namespace Domain.Common;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum OccupancyLevel { Low, Medium, High }
public enum RouteType { Trunk, Feeder, Express, Circular }
public enum BusType { Standard, Articulated, Feeder, Electric }
public enum BusStatus { InService, Delayed, OutOfService, Maintenance }
public enum IncidentSeverity { Low, Medium, High, Critical }
public enum IncidentType { Congestion, Delay, RoadBlock, BusFailure, Security, Weather, Operational }
public enum AlertType { Info, Warning, Critical, Recommendation }
public enum RecommendationType { DispatchBus, AdjustFrequency, TransferSuggestion, Reroute }
