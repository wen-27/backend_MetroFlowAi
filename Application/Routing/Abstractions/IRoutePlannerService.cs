using Application.Routing.Models;

namespace Application.Routing.Abstractions;

public interface IRoutePlannerService
{
    Task<RoutePlanResult> PlanAsync(RoutePlanRequest request, CancellationToken cancellationToken = default);
}

