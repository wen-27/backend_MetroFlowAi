using Domain.Common;

namespace Domain.Alerts.Entities;

public sealed class Alert : Entity
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public AlertType AlertType { get; set; } = AlertType.Info;
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Low;
    public Guid? RouteId { get; set; }
    public Guid? StationId { get; set; }
    public bool IsActive { get; set; } = true;
}

