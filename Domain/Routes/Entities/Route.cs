using Domain.Common;

namespace Domain.Routes.Entities;

public sealed class Route : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
    public int ActiveBuses { get; set; }
    public int AvgTimeMinutes { get; set; }
    public int DelayMinutes { get; set; }
    public OccupancyLevel OccupancyLevel { get; set; } = OccupancyLevel.Low;
    public string FrontendStatus { get; set; } = "normal";
    public string PathCoordinatesJson { get; set; } = "[]";
    public RouteType RouteType { get; set; } = RouteType.Trunk;
    public bool IsActive { get; set; } = true;
}
