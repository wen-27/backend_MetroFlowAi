using Application.Common;
using Application.Routing.Abstractions;
using Application.Routing.Models;
using Application.VectorSearch.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Public.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicController(IMetroFlowDbContext db, IRoutePlannerService planner, IVectorSearchService vectorSearch) : ControllerBase
{
    [HttpGet("stations")]
    public async Task<IActionResult> Stations(CancellationToken ct) =>
        Ok(await db.Stations.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));

    [HttpGet("routes")]
    public async Task<IActionResult> Routes(CancellationToken ct) =>
        Ok(await db.Routes.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct));

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts(CancellationToken ct) =>
        Ok(await db.Alerts.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.CreatedAt).ToListAsync(ct));

    [HttpGet("stations/{stationId:guid}/arrivals")]
    public async Task<IActionResult> Arrivals(Guid stationId, CancellationToken ct)
    {
        var arrivals = await db.ArrivalPredictions.AsNoTracking()
            .Where(x => x.StationId == stationId)
            .Join(db.Routes, p => p.RouteId, r => r.Id, (p, r) => new
            {
                routeId = r.Id,
                routeCode = r.Code,
                routeName = r.Name,
                p.EstimatedArrivalMinutes,
                occupancyLevel = p.OccupancyLevel.ToString(),
                p.Confidence
            })
            .OrderBy(x => x.EstimatedArrivalMinutes)
            .ToListAsync(ct);
        return Ok(arrivals);
    }

    [HttpGet("routes/{routeId:guid}/occupancy")]
    public async Task<IActionResult> Occupancy(Guid routeId, CancellationToken ct)
    {
        var buses = await db.Buses.AsNoTracking().Where(x => x.AssignedRouteId == routeId).ToListAsync(ct);
        return Ok(new
        {
            routeId,
            occupancyLevel = buses.Select(x => x.OccupancyLevel).DefaultIfEmpty().Max().ToString(),
            buses = buses.Select(x => new { x.InternalCode, x.Plate, x.CurrentOccupancy, occupancyLevel = x.OccupancyLevel.ToString(), status = x.Status.ToString() })
        });
    }

    [HttpPost("route-plan")]
    public async Task<IActionResult> RoutePlan([FromBody] RoutePlanRequest request, CancellationToken ct) =>
        Ok(await planner.PlanAsync(request, ct));

    [HttpPost("semantic-search")]
    public async Task<IActionResult> SemanticSearch([FromBody] SemanticSearchRequest request, CancellationToken ct) =>
        Ok(await vectorSearch.SearchAsync(request.Query, request.Limit <= 0 ? 5 : request.Limit, ct));
}

public sealed record SemanticSearchRequest(string Query, int Limit = 5);

