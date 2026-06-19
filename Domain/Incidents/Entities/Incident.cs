using Domain.Common;

namespace Domain.Incidents.Entities;

public sealed class Incident : Entity
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Low;
    public IncidentType IncidentType { get; set; } = IncidentType.Operational;
    public Guid? RouteId { get; set; }
    public Guid? StationId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public void Resolve()
    {
        IsActive = false;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

