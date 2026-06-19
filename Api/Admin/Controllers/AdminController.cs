using Application.Common;
using Application.Routing.Abstractions;
using Application.VectorSearch.Abstractions;
using Domain.Incidents.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Admin.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(
    IMetroFlowDbContext db,
    IVectorIndexingService vectorIndexing,
    IRouteGraphService graph) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var buses = await db.Buses.AsNoTracking().ToListAsync(ct);
        var positions = await db.BusPositions.AsNoTracking().ToListAsync(ct);
        var routes = await db.Routes.AsNoTracking().ToListAsync(ct);
        var incidents = await db.Incidents.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        return Ok(new
        {
            totalBuses = buses.Count,
            busesInService = buses.Count(x => x.Status.ToString() == "InService"),
            busesDelayed = buses.Count(x => x.Status.ToString() == "Delayed"),
            congestedStations = await db.Stations.CountAsync(x => x.CurrentOccupancyLevel.ToString() == "High", ct),
            routesWithIncidents = incidents.Select(x => x.RouteId).Where(x => x != null).Distinct().Count(),
            activeAlerts = await db.Alerts.CountAsync(x => x.IsActive, ct),
            pendingRecommendations = await db.OperationalRecommendations.CountAsync(x => !x.IsResolved, ct),
            busesMap = from bus in buses
                       join pos in positions on bus.Id equals pos.BusId into bp
                       from pos in bp.DefaultIfEmpty()
                       select new { bus.InternalCode, bus.Plate, status = bus.Status.ToString(), pos?.Latitude, pos?.Longitude },
            criticalRoutes = routes.Where(r => incidents.Any(i => i.RouteId == r.Id)).Select(r => new { r.Id, r.Code, r.Name })
        });
    }

    [HttpGet("buses")]
    public async Task<IActionResult> Buses(CancellationToken ct) =>
        Ok(await db.Buses.AsNoTracking().OrderBy(x => x.InternalCode).ToListAsync(ct));

    [HttpGet("buses/map")]
    public async Task<IActionResult> BusesMap(CancellationToken ct) =>
        Ok(await db.BusPositions.AsNoTracking().Join(db.Buses, p => p.BusId, b => b.Id, (p, b) => new
        {
            b.Id,
            b.InternalCode,
            b.Plate,
            status = b.Status.ToString(),
            p.Latitude,
            p.Longitude,
            p.SpeedKmh,
            p.ReportedAt
        }).ToListAsync(ct));

    [HttpGet("routes/status")]
    public async Task<IActionResult> RouteStatus(CancellationToken ct) =>
        Ok(await db.Routes.AsNoTracking().Select(r => new
        {
            r.Id,
            r.Code,
            r.Name,
            activeIncidents = db.Incidents.Count(i => i.IsActive && i.RouteId == r.Id),
            buses = db.Buses.Count(b => b.AssignedRouteId == r.Id)
        }).ToListAsync(ct));

    [HttpGet("stations/congestion")]
    public async Task<IActionResult> StationCongestion(CancellationToken ct) =>
        Ok(await db.Stations.AsNoTracking().OrderByDescending(x => x.CurrentOccupancyLevel).Select(x => new { x.Id, x.Code, x.Name, x.Sector, occupancyLevel = x.CurrentOccupancyLevel.ToString() }).ToListAsync(ct));

    [HttpGet("incidents")]
    public async Task<IActionResult> Incidents(CancellationToken ct) =>
        Ok(await db.Incidents.AsNoTracking().OrderByDescending(x => x.StartedAt).ToListAsync(ct));

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken ct)
    {
        var incident = new Incident
        {
            Title = request.Title,
            Description = request.Description,
            Severity = request.Severity,
            IncidentType = request.IncidentType,
            RouteId = request.RouteId,
            StationId = request.StationId
        };
        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);
        graph.Clear();
        return Created($"/api/admin/incidents/{incident.Id}", incident);
    }

    [HttpPut("incidents/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveIncident(Guid id, CancellationToken ct)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (incident is null) return NotFound();
        incident.Resolve();
        await db.SaveChangesAsync(ct);
        graph.Clear();
        return Ok(incident);
    }

    [HttpGet("predictions/demand")]
    public async Task<IActionResult> Demand(CancellationToken ct) =>
        Ok(await db.ArrivalPredictions.AsNoTracking().GroupBy(x => x.StationId).Select(x => new { stationId = x.Key, averageEta = x.Average(p => p.EstimatedArrivalMinutes), predictions = x.Count() }).ToListAsync(ct));

    [HttpGet("recommendations")]
    public async Task<IActionResult> Recommendations(CancellationToken ct) =>
        Ok(await db.OperationalRecommendations.AsNoTracking().OrderBy(x => x.IsResolved).ThenBy(x => x.Priority).ToListAsync(ct));

    [HttpPost("recommendations/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveRecommendation(Guid id, CancellationToken ct)
    {
        var recommendation = await db.OperationalRecommendations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (recommendation is null) return NotFound();
        recommendation.IsResolved = true;
        recommendation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(recommendation);
    }

    [HttpPost("vector/reindex")]
    public async Task<IActionResult> Reindex(CancellationToken ct) =>
        Ok(new { indexedDocuments = await vectorIndexing.ReindexAsync(ct) });

    [HttpGet("graph/status")]
    public async Task<IActionResult> GraphStatus(CancellationToken ct)
    {
        var built = await graph.BuildGraphAsync(cancellationToken: ct);
        return Ok(new { nodes = built.Count, edges = built.Values.Sum(x => x.Count), cached = true });
    }

    [HttpPost("graph/rebuild")]
    public async Task<IActionResult> RebuildGraph(CancellationToken ct)
    {
        graph.Clear();
        var built = await graph.BuildGraphAsync(forceRebuild: true, cancellationToken: ct);
        return Ok(new { nodes = built.Count, edges = built.Values.Sum(x => x.Count), rebuilt = true });
    }
}

public sealed record CreateIncidentRequest(string Title, string Description, Domain.Common.IncidentSeverity Severity, Domain.Common.IncidentType IncidentType, Guid? RouteId, Guid? StationId);

