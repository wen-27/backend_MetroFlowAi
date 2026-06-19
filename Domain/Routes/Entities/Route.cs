using Domain.Common;

namespace Domain.Routes.Entities;

public sealed class Route : Entity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public RouteType RouteType { get; set; } = RouteType.Trunk;
    public bool IsActive { get; set; } = true;
}

