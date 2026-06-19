using System.Text.Json;
using Application.Common;
using Domain.Alerts.Entities;
using Domain.Buses.Entities;
using Domain.Common;
using Domain.Incidents.Entities;
using Infrastructure.Seed;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Public.Controllers;

[ApiController]
[Route("api/app")]
public sealed class AppController(IMetroFlowDbContext db, MetroFlowSeeder seeder) : ControllerBase
{
    [HttpGet("state")]
    public async Task<IActionResult> State(CancellationToken ct) =>
        Ok(new
        {
            routes = await FrontendRoutes(ct),
            stations = await FrontendStations(ct),
            buses = await FrontendBuses(ct),
            alerts = await FrontendAlerts(ct),
            incidents = await FrontendIncidents(ct),
            recommendations = await FrontendRecommendations(ct),
            peakDemandForecast = PeakDemandForecast()
        });

    [HttpPost("route-search")]
    public async Task<IActionResult> RouteSearch([FromBody] RouteSearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Origin) || string.IsNullOrWhiteSpace(request.Destination)) return BadRequest();

        var stations = await FrontendStations(ct);
        var routes = await FrontendRoutes(ct);
        var buses = await FrontendBuses(ct);
        var origin = stations.FirstOrDefault(x => x.name.Contains(request.Origin, StringComparison.OrdinalIgnoreCase)) ?? stations.First();
        var destination = stations.FirstOrDefault(x => x.name.Contains(request.Destination, StringComparison.OrdinalIgnoreCase)) ?? stations.Skip(2).First();
        var route = routes.FirstOrDefault(x =>
            ((string)x.name).Contains((string)origin.name, StringComparison.OrdinalIgnoreCase) ||
            ((string)x.name).Contains((string)destination.name, StringComparison.OrdinalIgnoreCase)) ?? routes.First();
        var nextBus = buses.FirstOrDefault(x => x.routeId == route.id);
        var walkMinutes = origin.id == destination.id ? 1 : 4;

        return Ok(new
        {
            originName = origin.name,
            destName = destination.name,
            routeCode = route.id,
            routeName = route.name,
            totalTime = route.avgTimeMinutes + route.delayMinutes + walkMinutes,
            nextArrivalMinutes = nextBus?.etaMinutes ?? 6,
            transfers = origin.id == destination.id ? 0 : 1,
            walkTime = walkMinutes,
            estimatedOccupancy = nextBus?.occupancy ?? route.occupancy,
            confidenceScore = 94,
            status = route.status,
            aiAdvice = $"La inteligencia artificial detecta que la estación {origin.name} tiene un {origin.occupancyCurrent}% de ocupación. El tiempo total aproximado es de {route.avgTimeMinutes + route.delayMinutes + walkMinutes} minutos."
        });
    }

    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] AppIncidentRequest request, CancellationToken ct)
    {
        var route = await db.Routes.FirstOrDefaultAsync(x => request.AffectedRoute.Contains(x.Code), ct);
        var station = await db.Stations.FirstOrDefaultAsync(x => request.Location.Contains(x.Name.Replace("Portal ", "")), ct);
        var incident = new Incident
        {
            ExternalCode = $"INC-{100 + await db.Incidents.CountAsync(ct) + 1}",
            Title = request.Type,
            Description = $"{request.Type} en {request.Location}",
            Location = request.Location,
            AffectedRoute = request.AffectedRoute,
            OfficerInCharge = request.OfficerInCharge,
            FrontendStatus = request.Status,
            ActiveDurationMinutes = 1,
            Severity = IncidentSeverity.High,
            IncidentType = IncidentType.Operational,
            RouteId = route?.Id,
            StationId = station?.Id
        };
        db.Incidents.Add(incident);
        db.Alerts.Add(new Alert
        {
            ExternalCode = $"AL-{await db.Alerts.CountAsync(ct) + 1}",
            Title = "incidente",
            Message = $"INCIDENTE DETECTADO: {incident.Title} en {incident.Location} afectando a {incident.AffectedRoute}.",
            Target = incident.AffectedRoute,
            Recommendation = "Evitar este tramo. Agentes de transito asignados. Se recomienda ajuste dinamico de frecuencia.",
            FrontendStatus = "new",
            AlertType = AlertType.Critical,
            Severity = IncidentSeverity.Critical,
            RouteId = route?.Id,
            StationId = station?.Id
        });
        await db.SaveChangesAsync(ct);
        return Ok((await FrontendIncidents(ct)).First(x => x.id == incident.ExternalCode));
    }

    [HttpPut("incidents/{code}/resolve")]
    public async Task<IActionResult> ResolveIncident(string code, CancellationToken ct)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(x => x.ExternalCode == code, ct);
        if (incident is null) return NotFound();
        incident.Resolve();
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPut("alerts/{code}/status")]
    public async Task<IActionResult> UpdateAlertStatus(string code, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var alert = await db.Alerts.FirstOrDefaultAsync(x => x.ExternalCode == code, ct);
        if (alert is null) return NotFound();
        alert.FrontendStatus = request.Status;
        alert.IsActive = request.Status != "resolved";
        alert.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("recommendations/{code}/apply")]
    public async Task<IActionResult> ApplyRecommendation(string code, CancellationToken ct)
    {
        var recommendation = await db.OperationalRecommendations.FirstOrDefaultAsync(x => x.ExternalCode == code, ct);
        if (recommendation is null) return NotFound();
        recommendation.IsResolved = true;
        recommendation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("routes/{routeCode}/simulate-bus")]
    public async Task<IActionResult> SimulateBus(string routeCode, CancellationToken ct)
    {
        var route = await db.Routes.FirstOrDefaultAsync(x => x.Code == routeCode, ct);
        if (route is null) return NotFound();
        var busCode = $"BUS-{Random.Shared.Next(800, 999)}";
        var bus = new Bus
        {
            InternalCode = busCode,
            Plate = busCode,
            DriverName = "Conductor de Apoyo IA",
            AssignedRouteId = route.Id,
            Capacity = 90,
            CurrentOccupancy = 28,
            OccupancyLevel = OccupancyLevel.Low,
            NextStation = route.Origin,
            EtaMinutes = 4,
            Status = BusStatus.InService
        };
        db.Buses.Add(bus);
        db.BusPositions.Add(new Domain.BusPositions.Entities.BusPosition { BusId = bus.Id, Latitude = 7.1085m, Longitude = -73.1180m, SpeedKmh = 25 });
        route.ActiveBuses += 1;
        route.DelayMinutes = Math.Max(0, route.DelayMinutes - 4);
        route.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        await seeder.ResetAsync(ct);
        return Ok();
    }

    private async Task<List<dynamic>> FrontendRoutes(CancellationToken ct)
    {
        var routes = await db.Routes.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        return routes.Select(x => (dynamic)new
        {
            id = x.Code,
            name = x.Name,
            origin = x.Origin,
            destination = x.Destination,
            activeBuses = x.ActiveBuses,
            avgTimeMinutes = x.AvgTimeMinutes,
            delayMinutes = x.DelayMinutes,
            occupancy = ToFrontendOccupancy(x.OccupancyLevel),
            status = x.FrontendStatus,
            pathCoordinates = JsonSerializer.Deserialize<decimal[][]>(x.PathCoordinatesJson) ?? []
        }).ToList();
    }

    private async Task<List<dynamic>> FrontendStations(CancellationToken ct)
    {
        var stations = await db.Stations.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        return stations.Select(x => (dynamic)new
        {
            id = x.Code,
            name = x.Name,
            occupancyCurrent = x.OccupancyCurrent,
            occupancyPrediction20Min = x.OccupancyPrediction20Min,
            riskLevel = ToFrontendOccupancy(x.CurrentOccupancyLevel),
            recommendation = x.Recommendation,
            capacity = x.Capacity
        }).ToList();
    }

    private async Task<List<dynamic>> FrontendBuses(CancellationToken ct)
    {
        var buses = await db.Buses.AsNoTracking().Join(db.Routes, b => b.AssignedRouteId, r => r.Id, (b, r) => new { b, r })
            .GroupJoin(db.BusPositions, br => br.b.Id, p => p.BusId, (br, positions) => new { br.b, br.r, position = positions.OrderByDescending(x => x.ReportedAt).FirstOrDefault() })
            .OrderBy(x => x.b.InternalCode)
            .ToListAsync(ct);
        return buses.Select(x => (dynamic)new
        {
            id = x.b.InternalCode,
            routeId = x.r.Code,
            driverName = x.b.DriverName,
            latitude = x.position?.Latitude ?? 7.1085m,
            longitude = x.position?.Longitude ?? -73.1180m,
            occupancy = ToFrontendOccupancy(x.b.OccupancyLevel),
            status = x.b.Status == BusStatus.Maintenance ? "maintenance" : x.b.Status == BusStatus.Delayed ? "delayed" : "active",
            nextStation = x.b.NextStation,
            etaMinutes = x.b.EtaMinutes
        }).ToList();
    }

    private async Task<List<dynamic>> FrontendAlerts(CancellationToken ct)
    {
        var alerts = await db.Alerts.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return alerts.Select(x => (dynamic)new
        {
            id = x.ExternalCode,
            type = x.Title,
            target = x.Target,
            level = x.Severity == IncidentSeverity.Critical ? "critical" : x.Severity == IncidentSeverity.Low ? "info" : "warning",
            description = x.Message,
            recommendation = x.Recommendation,
            timestamp = x.CreatedAt.ToLocalTime().ToString("hh:mm tt"),
            status = x.FrontendStatus
        }).ToList();
    }

    private async Task<List<dynamic>> FrontendIncidents(CancellationToken ct)
    {
        var incidents = await db.Incidents.AsNoTracking().OrderByDescending(x => x.StartedAt).ToListAsync(ct);
        return incidents.Select(x => (dynamic)new
        {
            id = x.ExternalCode,
            type = x.Title,
            location = x.Location,
            affectedRoute = x.AffectedRoute,
            status = x.FrontendStatus,
            activeDurationMinutes = x.ActiveDurationMinutes,
            officerInCharge = x.OfficerInCharge
        }).ToList();
    }

    private async Task<List<dynamic>> FrontendRecommendations(CancellationToken ct)
    {
        var recommendations = await db.OperationalRecommendations.AsNoTracking().OrderBy(x => x.Priority).ToListAsync(ct);
        return recommendations.Select(x => (dynamic)new
        {
            id = x.ExternalCode,
            title = x.Title,
            impact = x.Impact,
            priority = x.Priority switch { 0 => "critical", 1 => "high", 2 => "medium", _ => "low" },
            suggestion = x.Description,
            applied = x.IsResolved,
            type = x.FrontendType,
            targetId = x.TargetCode
        }).ToList();
    }

    private static string ToFrontendOccupancy(OccupancyLevel level) =>
        level switch
        {
            OccupancyLevel.Critical => "critical",
            OccupancyLevel.High => "high",
            OccupancyLevel.Medium => "medium",
            _ => "low"
        };

    private static object[] PeakDemandForecast() =>
    [
        new { hour = "06:00", passengers = 3200, capacity = 4000, risk = "low" },
        new { hour = "07:00", passengers = 5900, capacity = 5500, risk = "critical" },
        new { hour = "08:00", passengers = 6300, capacity = 5500, risk = "critical" },
        new { hour = "09:00", passengers = 4200, capacity = 5000, risk = "high" },
        new { hour = "10:00", passengers = 2800, capacity = 4500, risk = "low" },
        new { hour = "11:00", passengers = 3100, capacity = 4500, risk = "low" },
        new { hour = "12:00", passengers = 3900, capacity = 4500, risk = "medium" },
        new { hour = "13:00", passengers = 3600, capacity = 4500, risk = "low" },
        new { hour = "14:00", passengers = 2900, capacity = 4500, risk = "low" },
        new { hour = "15:00", passengers = 3300, capacity = 4500, risk = "low" },
        new { hour = "16:00", passengers = 4100, capacity = 4500, risk = "medium" },
        new { hour = "17:00", passengers = 5800, capacity = 5200, risk = "high" },
        new { hour = "18:00", passengers = 6800, capacity = 5500, risk = "critical" },
        new { hour = "19:00", passengers = 5200, capacity = 5500, risk = "medium" },
        new { hour = "20:00", passengers = 3000, capacity = 4000, risk = "low" }
    ];
}

public sealed record RouteSearchRequest(string Origin, string Destination);
public sealed record UpdateStatusRequest(string Status);
public sealed record AppIncidentRequest(string Type, string Location, string AffectedRoute, string Status, string OfficerInCharge);
