using Domain.Common;

namespace Domain.Alerts.Entities;

public sealed class Alert : Entity
{
    public string ExternalCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Target { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public string FrontendStatus { get; set; } = "new";
    public AlertType AlertType { get; set; } = AlertType.Info;
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Low;
    public Guid? RouteId { get; set; }
    public Guid? StationId { get; set; }
    public bool IsActive { get; set; } = true;
}
