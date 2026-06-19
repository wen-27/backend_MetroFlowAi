using Domain.Common;

namespace Domain.Stations.Entities;

public sealed class Station : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Sector { get; set; } = "";
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public OccupancyLevel CurrentOccupancyLevel { get; set; } = OccupancyLevel.Low;
}

