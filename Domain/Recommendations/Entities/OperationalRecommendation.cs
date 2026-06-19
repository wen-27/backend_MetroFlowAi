using Domain.Common;

namespace Domain.Recommendations.Entities;

public sealed class OperationalRecommendation : Entity
{
    public string ExternalCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Impact { get; set; } = "";
    public string FrontendType { get; set; } = "";
    public string TargetCode { get; set; } = "";
    public RecommendationType RecommendationType { get; set; } = RecommendationType.DispatchBus;
    public Guid? RouteId { get; set; }
    public Guid? StationId { get; set; }
    public int Priority { get; set; } = 1;
    public bool IsResolved { get; set; }
}
