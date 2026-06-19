using System.Text.Json;
using Application.Common;
using Application.Routing.Abstractions;
using Domain.Buses.Entities;
using Domain.BusPositions.Entities;
using Domain.Common;
using Domain.RouteSegments.Entities;
using Domain.RouteStations.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetroRoute = Domain.Routes.Entities.Route;

namespace Api.Admin.Controllers;

[ApiController]
[Route("api/admin/manage")]
public sealed class FleetManagementController(IMetroFlowDbContext db, IRouteGraphService graph) : ControllerBase
{
    [HttpGet("routes")]
    public async Task<IActionResult> Routes(CancellationToken ct)
    {
        var routes = await db.Routes.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        var routeIds = routes.Select(x => x.Id).ToArray();
        var stations = await db.RouteStations.AsNoTracking()
            .Where(x => routeIds.Contains(x.RouteId))
            .Join(db.Stations, rs => rs.StationId, s => s.Id, (rs, s) => new
            {
                rs.RouteId,
                s.Code,
                s.Name,
                rs.StopOrder,
                rs.EstimatedMinutesFromStart,
                rs.IsTransferPoint
            })
            .OrderBy(x => x.StopOrder)
            .ToListAsync(ct);
        var busesByRoute = await db.Buses.AsNoTracking()
            .GroupBy(x => x.AssignedRouteId)
            .Select(x => new { RouteId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.RouteId, x => x.Count, ct);

        return Ok(routes.Select(route => new
        {
            id = route.Code,
            databaseId = route.Id,
            route.Code,
            route.Name,
            route.Origin,
            route.Destination,
            route.AvgTimeMinutes,
            route.DelayMinutes,
            occupancy = ToFrontendOccupancy(route.OccupancyLevel),
            status = route.FrontendStatus,
            routeType = route.RouteType.ToString(),
            activeBuses = busesByRoute.GetValueOrDefault(route.Id),
            stations = stations.Where(x => x.RouteId == route.Id).Select(x => new
            {
                x.Code,
                x.Name,
                x.StopOrder,
                x.EstimatedMinutesFromStart,
                x.IsTransferPoint
            })
        }));
    }

    [HttpPost("routes")]
    public async Task<IActionResult> CreateRoute([FromBody] UpsertRouteRequest request, CancellationToken ct)
    {
        var validation = await ValidateRouteRequest(request, null, ct);
        if (validation is not null) return validation;

        var selectedStations = await GetStations(request.StationCodes, ct);
        var code = string.IsNullOrWhiteSpace(request.Code) ? await NextRouteCode(ct) : request.Code.Trim().ToUpperInvariant();
        if (await db.Routes.AnyAsync(x => x.Code == code, ct))
            return Conflict(new { message = $"Ya existe una ruta con codigo {code}." });

        var route = new MetroRoute
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? $"{selectedStations.First().Name} hacia {selectedStations.Last().Name}",
            Origin = selectedStations.First().Name,
            Destination = selectedStations.Last().Name,
            AvgTimeMinutes = request.AvgTimeMinutes <= 0 ? Math.Max(8, (selectedStations.Count - 1) * 6) : request.AvgTimeMinutes,
            DelayMinutes = Math.Max(0, request.DelayMinutes),
            OccupancyLevel = ParseOccupancy(request.Occupancy),
            FrontendStatus = string.IsNullOrWhiteSpace(request.Status) ? "normal" : request.Status.Trim(),
            RouteType = ParseRouteType(request.RouteType),
            PathCoordinatesJson = BuildPathCoordinates(selectedStations),
            IsActive = true
        };

        db.Routes.Add(route);
        AddRouteTopology(route, selectedStations);
        await db.SaveChangesAsync(ct);
        graph.Clear();

        return Created($"/api/admin/manage/routes/{route.Code}", await RouteDetails(route.Code, ct));
    }

    [HttpPut("routes/{code}")]
    public async Task<IActionResult> UpdateRoute(string code, [FromBody] UpsertRouteRequest request, CancellationToken ct)
    {
        var route = await db.Routes.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (route is null) return NotFound();

        var validation = await ValidateRouteRequest(request, route.Id, ct);
        if (validation is not null) return validation;

        var selectedStations = await GetStations(request.StationCodes, ct);
        var newCode = string.IsNullOrWhiteSpace(request.Code) ? route.Code : request.Code.Trim().ToUpperInvariant();
        if (!string.Equals(newCode, route.Code, StringComparison.OrdinalIgnoreCase) &&
            await db.Routes.AnyAsync(x => x.Code == newCode, ct))
            return Conflict(new { message = $"Ya existe una ruta con codigo {newCode}." });

        route.Code = newCode;
        route.Name = request.Name.Trim();
        route.Description = request.Description?.Trim() ?? $"{selectedStations.First().Name} hacia {selectedStations.Last().Name}";
        route.Origin = selectedStations.First().Name;
        route.Destination = selectedStations.Last().Name;
        route.AvgTimeMinutes = request.AvgTimeMinutes <= 0 ? Math.Max(8, (selectedStations.Count - 1) * 6) : request.AvgTimeMinutes;
        route.DelayMinutes = Math.Max(0, request.DelayMinutes);
        route.OccupancyLevel = ParseOccupancy(request.Occupancy);
        route.FrontendStatus = string.IsNullOrWhiteSpace(request.Status) ? "normal" : request.Status.Trim();
        route.RouteType = ParseRouteType(request.RouteType);
        route.PathCoordinatesJson = BuildPathCoordinates(selectedStations);
        route.UpdatedAt = DateTime.UtcNow;

        db.RouteStations.RemoveRange(await db.RouteStations.Where(x => x.RouteId == route.Id).ToListAsync(ct));
        db.RouteSegments.RemoveRange(await db.RouteSegments.Where(x => x.RouteId == route.Id).ToListAsync(ct));
        AddRouteTopology(route, selectedStations);
        await db.SaveChangesAsync(ct);
        graph.Clear();

        return Ok(await RouteDetails(route.Code, ct));
    }

    [HttpDelete("routes/{code}")]
    public async Task<IActionResult> DeleteRoute(string code, CancellationToken ct)
    {
        var route = await db.Routes.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (route is null) return NotFound();
        if (await db.Buses.AnyAsync(x => x.AssignedRouteId == route.Id, ct))
            return Conflict(new { message = "No se puede eliminar una ruta con buses asignados. Reasigna o elimina esos buses primero." });

        db.RouteStations.RemoveRange(await db.RouteStations.Where(x => x.RouteId == route.Id).ToListAsync(ct));
        db.RouteSegments.RemoveRange(await db.RouteSegments.Where(x => x.RouteId == route.Id).ToListAsync(ct));
        db.Routes.Remove(route);
        await db.SaveChangesAsync(ct);
        graph.Clear();
        return NoContent();
    }

    [HttpGet("buses")]
    public async Task<IActionResult> Buses(CancellationToken ct)
    {
        var buses = await db.Buses.AsNoTracking()
            .Join(db.Routes, bus => bus.AssignedRouteId, route => route.Id, (bus, route) => new { bus, route })
            .GroupJoin(db.BusPositions, br => br.bus.Id, pos => pos.BusId, (br, positions) => new
            {
                br.bus,
                br.route,
                position = positions.OrderByDescending(x => x.ReportedAt).FirstOrDefault()
            })
            .OrderBy(x => x.bus.InternalCode)
            .ToListAsync(ct);

        return Ok(buses.Select(x => new
        {
            id = x.bus.InternalCode,
            databaseId = x.bus.Id,
            x.bus.InternalCode,
            x.bus.Plate,
            x.bus.DriverName,
            busType = x.bus.BusType.ToString(),
            x.bus.Capacity,
            x.bus.CurrentOccupancy,
            occupancy = ToFrontendOccupancy(x.bus.OccupancyLevel),
            routeId = x.route.Code,
            routeName = x.route.Name,
            x.bus.NextStation,
            x.bus.EtaMinutes,
            status = ToFrontendBusStatus(x.bus.Status),
            latitude = x.position?.Latitude,
            longitude = x.position?.Longitude
        }));
    }

    [HttpPost("buses")]
    public async Task<IActionResult> CreateBus([FromBody] UpsertBusRequest request, CancellationToken ct)
    {
        var validation = await ValidateBusRequest(request, null, ct);
        if (validation is not null) return validation;
        if (await db.Buses.AnyAsync(x => x.InternalCode == request.InternalCode.Trim().ToUpperInvariant(), ct))
            return Conflict(new { message = $"Ya existe un bus con codigo {request.InternalCode}." });

        var route = await db.Routes.FirstAsync(x => x.Code == request.RouteCode.Trim().ToUpperInvariant(), ct);
        var nextStation = await ResolveNextStation(route.Id, request.NextStationCode, ct);
        var bus = new Bus
        {
            InternalCode = request.InternalCode.Trim().ToUpperInvariant(),
            Plate = string.IsNullOrWhiteSpace(request.Plate) ? request.InternalCode.Trim().ToUpperInvariant() : request.Plate.Trim().ToUpperInvariant(),
            DriverName = request.DriverName.Trim(),
            BusType = ParseBusType(request.BusType),
            Capacity = request.Capacity <= 0 ? 90 : request.Capacity,
            CurrentOccupancy = Math.Clamp(request.CurrentOccupancy, 0, Math.Max(1, request.Capacity <= 0 ? 90 : request.Capacity)),
            OccupancyLevel = ParseOccupancy(request.Occupancy),
            AssignedRouteId = route.Id,
            NextStation = nextStation.Name,
            EtaMinutes = Math.Max(0, request.EtaMinutes),
            Status = ParseBusStatus(request.Status)
        };

        db.Buses.Add(bus);
        db.BusPositions.Add(new BusPosition { BusId = bus.Id, Latitude = nextStation.Latitude, Longitude = nextStation.Longitude, SpeedKmh = 24, CurrentStationId = nextStation.Id });
        route.ActiveBuses = await db.Buses.CountAsync(x => x.AssignedRouteId == route.Id, ct) + 1;
        route.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Created($"/api/admin/manage/buses/{bus.InternalCode}", await BusDetails(bus.InternalCode, ct));
    }

    [HttpPut("buses/{code}")]
    public async Task<IActionResult> UpdateBus(string code, [FromBody] UpsertBusRequest request, CancellationToken ct)
    {
        var bus = await db.Buses.FirstOrDefaultAsync(x => x.InternalCode == code, ct);
        if (bus is null) return NotFound();

        var validation = await ValidateBusRequest(request, bus.Id, ct);
        if (validation is not null) return validation;

        var newCode = request.InternalCode.Trim().ToUpperInvariant();
        if (!string.Equals(newCode, bus.InternalCode, StringComparison.OrdinalIgnoreCase) &&
            await db.Buses.AnyAsync(x => x.InternalCode == newCode, ct))
            return Conflict(new { message = $"Ya existe un bus con codigo {newCode}." });

        var previousRouteId = bus.AssignedRouteId;
        var route = await db.Routes.FirstAsync(x => x.Code == request.RouteCode.Trim().ToUpperInvariant(), ct);
        var nextStation = await ResolveNextStation(route.Id, request.NextStationCode, ct);

        bus.InternalCode = newCode;
        bus.Plate = string.IsNullOrWhiteSpace(request.Plate) ? newCode : request.Plate.Trim().ToUpperInvariant();
        bus.DriverName = request.DriverName.Trim();
        bus.BusType = ParseBusType(request.BusType);
        bus.Capacity = request.Capacity <= 0 ? 90 : request.Capacity;
        bus.CurrentOccupancy = Math.Clamp(request.CurrentOccupancy, 0, Math.Max(1, bus.Capacity));
        bus.OccupancyLevel = ParseOccupancy(request.Occupancy);
        bus.AssignedRouteId = route.Id;
        bus.NextStation = nextStation.Name;
        bus.EtaMinutes = Math.Max(0, request.EtaMinutes);
        bus.Status = ParseBusStatus(request.Status);
        bus.UpdatedAt = DateTime.UtcNow;

        db.BusPositions.Add(new BusPosition { BusId = bus.Id, Latitude = nextStation.Latitude, Longitude = nextStation.Longitude, SpeedKmh = 24, CurrentStationId = nextStation.Id });
        await RefreshRouteBusCounts([previousRouteId, route.Id], ct);
        await db.SaveChangesAsync(ct);
        return Ok(await BusDetails(bus.InternalCode, ct));
    }

    [HttpDelete("buses/{code}")]
    public async Task<IActionResult> DeleteBus(string code, CancellationToken ct)
    {
        var bus = await db.Buses.FirstOrDefaultAsync(x => x.InternalCode == code, ct);
        if (bus is null) return NotFound();
        var routeId = bus.AssignedRouteId;
        db.BusPositions.RemoveRange(await db.BusPositions.Where(x => x.BusId == bus.Id).ToListAsync(ct));
        db.Buses.Remove(bus);
        await db.SaveChangesAsync(ct);
        await RefreshRouteBusCounts([routeId], ct);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<IActionResult?> ValidateRouteRequest(UpsertRouteRequest request, Guid? routeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "El nombre de la ruta es obligatorio." });
        if (request.StationCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2)
            return BadRequest(new { message = "La ruta debe tener al menos dos estaciones diferentes." });

        var stationCodes = request.StationCodes.Select(x => x.Trim().ToUpperInvariant()).ToArray();
        var found = await db.Stations.CountAsync(x => stationCodes.Contains(x.Code), ct);
        if (found != stationCodes.Distinct().Count())
            return BadRequest(new { message = "Una o mas estaciones no existen." });
        return null;
    }

    private async Task<IActionResult?> ValidateBusRequest(UpsertBusRequest request, Guid? busId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.InternalCode))
            return BadRequest(new { message = "El codigo interno del bus es obligatorio." });
        if (string.IsNullOrWhiteSpace(request.DriverName))
            return BadRequest(new { message = "El nombre del conductor es obligatorio." });
        var route = await db.Routes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == request.RouteCode.Trim().ToUpperInvariant(), ct);
        if (route is null)
            return BadRequest(new { message = "La ruta asignada no existe." });
        if (!string.IsNullOrWhiteSpace(request.NextStationCode))
        {
            var validStop = await db.RouteStations.AsNoTracking()
                .Join(db.Stations, rs => rs.StationId, s => s.Id, (rs, s) => new { rs.RouteId, s.Code })
                .AnyAsync(x => x.RouteId == route.Id && x.Code == request.NextStationCode.Trim().ToUpperInvariant(), ct);
            if (!validStop)
                return BadRequest(new { message = "La proxima estacion debe pertenecer a la ruta seleccionada." });
        }
        return null;
    }

    private async Task<List<Domain.Stations.Entities.Station>> GetStations(IReadOnlyList<string> stationCodes, CancellationToken ct)
    {
        var normalized = stationCodes.Select(x => x.Trim().ToUpperInvariant()).Distinct().ToArray();
        var stations = await db.Stations.Where(x => normalized.Contains(x.Code)).ToListAsync(ct);
        return normalized.Select(code => stations.First(x => x.Code == code)).ToList();
    }

    private void AddRouteTopology(MetroRoute route, IReadOnlyList<Domain.Stations.Entities.Station> stations)
    {
        for (var index = 0; index < stations.Count; index++)
        {
            db.RouteStations.Add(new RouteStation
            {
                RouteId = route.Id,
                StationId = stations[index].Id,
                StopOrder = index + 1,
                EstimatedMinutesFromStart = index * 6,
                IsTransferPoint = index > 0 && index < stations.Count - 1
            });
        }

        for (var index = 0; index < stations.Count - 1; index++)
        {
            db.RouteSegments.Add(new RouteSegment
            {
                RouteId = route.Id,
                OriginStationId = stations[index].Id,
                DestinationStationId = stations[index + 1].Id,
                DistanceKm = EstimateDistanceKm(stations[index], stations[index + 1]),
                BaseTravelTimeMinutes = 5 + (index % 4),
                IsActive = true
            });
        }
    }

    private async Task<Domain.Stations.Entities.Station> ResolveNextStation(Guid routeId, string? stationCode, CancellationToken ct)
    {
        var query = db.RouteStations.AsNoTracking()
            .Where(x => x.RouteId == routeId)
            .Join(db.Stations, rs => rs.StationId, station => station.Id, (rs, station) => new { rs.StopOrder, station })
            .OrderBy(x => x.StopOrder);

        if (!string.IsNullOrWhiteSpace(stationCode))
            return (await query.FirstAsync(x => x.station.Code == stationCode.Trim().ToUpperInvariant(), ct)).station;

        return (await query.FirstAsync(ct)).station;
    }

    private async Task RefreshRouteBusCounts(IEnumerable<Guid> routeIds, CancellationToken ct)
    {
        foreach (var routeId in routeIds.Distinct())
        {
            var route = await db.Routes.FirstOrDefaultAsync(x => x.Id == routeId, ct);
            if (route is null) continue;
            route.ActiveBuses = await db.Buses.CountAsync(x => x.AssignedRouteId == routeId, ct);
            route.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<object?> RouteDetails(string code, CancellationToken ct) =>
        (await Routes(ct) as OkObjectResult)?.Value is IEnumerable<object> routes
            ? routes.FirstOrDefault(x => string.Equals((string?)x.GetType().GetProperty("Code")?.GetValue(x), code, StringComparison.OrdinalIgnoreCase))
            : null;

    private async Task<object?> BusDetails(string code, CancellationToken ct) =>
        (await Buses(ct) as OkObjectResult)?.Value is IEnumerable<object> buses
            ? buses.FirstOrDefault(x => string.Equals((string?)x.GetType().GetProperty("InternalCode")?.GetValue(x), code, StringComparison.OrdinalIgnoreCase))
            : null;

    private async Task<string> NextRouteCode(CancellationToken ct)
    {
        var codes = await db.Routes.AsNoTracking().Select(x => x.Code).ToListAsync(ct);
        var next = codes
            .Select(code => int.TryParse(code.TrimStart('R'), out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
        return $"R{next}";
    }

    private static string BuildPathCoordinates(IEnumerable<Domain.Stations.Entities.Station> stations) =>
        JsonSerializer.Serialize(stations.Select(x => new[] { x.Latitude, x.Longitude }));

    private static decimal EstimateDistanceKm(Domain.Stations.Entities.Station origin, Domain.Stations.Entities.Station destination)
    {
        var lat = Math.Abs(origin.Latitude - destination.Latitude);
        var lng = Math.Abs(origin.Longitude - destination.Longitude);
        return Math.Max(0.3m, Math.Round((lat + lng) * 85m, 2));
    }

    private static OccupancyLevel ParseOccupancy(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "critical" => OccupancyLevel.Critical,
            "high" => OccupancyLevel.High,
            "medium" => OccupancyLevel.Medium,
            _ => OccupancyLevel.Low
        };

    private static string ToFrontendOccupancy(OccupancyLevel level) =>
        level switch
        {
            OccupancyLevel.Critical => "critical",
            OccupancyLevel.High => "high",
            OccupancyLevel.Medium => "medium",
            _ => "low"
        };

    private static RouteType ParseRouteType(string? value) =>
        Enum.TryParse<RouteType>(value, true, out var parsed) ? parsed : RouteType.Trunk;

    private static BusType ParseBusType(string? value) =>
        Enum.TryParse<BusType>(value, true, out var parsed) ? parsed : BusType.Standard;

    private static BusStatus ParseBusStatus(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "delayed" => BusStatus.Delayed,
            "maintenance" => BusStatus.Maintenance,
            "outofservice" or "out_of_service" => BusStatus.OutOfService,
            _ => BusStatus.InService
        };

    private static string ToFrontendBusStatus(BusStatus status) =>
        status switch
        {
            BusStatus.Maintenance => "maintenance",
            BusStatus.Delayed => "delayed",
            BusStatus.OutOfService => "maintenance",
            _ => "active"
        };
}

public sealed record UpsertRouteRequest(
    string? Code,
    string Name,
    string? Description,
    IReadOnlyList<string> StationCodes,
    int AvgTimeMinutes,
    int DelayMinutes,
    string? Occupancy,
    string? Status,
    string? RouteType);

public sealed record UpsertBusRequest(
    string InternalCode,
    string? Plate,
    string DriverName,
    string? BusType,
    int Capacity,
    int CurrentOccupancy,
    string? Occupancy,
    string RouteCode,
    string? NextStationCode,
    int EtaMinutes,
    string? Status);
