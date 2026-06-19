using System.Text.Json;
using Domain.Alerts.Entities;
using Domain.Buses.Entities;
using Domain.BusPositions.Entities;
using Domain.Common;
using Domain.Incidents.Entities;
using Domain.Predictions.Entities;
using Domain.Recommendations.Entities;
using Domain.Routes.Entities;
using Domain.RouteSegments.Entities;
using Domain.RouteStations.Entities;
using Domain.Stations.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Seed;

public sealed class MetroFlowSeeder(MetroFlowDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Routes.AnyAsync(x => x.Code == "R1", cancellationToken)) return;

        await SeedFrontendDataAsync(cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await SeedFrontendDataAsync(cancellationToken);
    }

    private async Task SeedFrontendDataAsync(CancellationToken cancellationToken)
    {
        await ClearDemoDataAsync(cancellationToken);

        var seed = await LoadSeedAsync(cancellationToken);
        var stations = seed.Stations.Select(ToStation).ToArray();
        db.Stations.AddRange(stations);

        var routes = seed.Routes.Select(ToRoute).ToArray();
        db.Routes.AddRange(routes);
        await db.SaveChangesAsync(cancellationToken);

        var stationByCode = stations.ToDictionary(x => x.Code);
        var stationByName = stations.ToDictionary(x => x.Name);
        var routeByCode = routes.ToDictionary(x => x.Code);

        AddRouteTopology(routes, stationByName);
        AddBuses(seed.Buses, routeByCode, stations);
        AddAlerts(seed.Alerts, routeByCode, stationByName);
        AddIncidents(seed.Incidents, routeByCode, stationByName);
        AddRecommendations(seed.Recommendations, routeByCode, stationByCode);
        AddArrivalPredictions(stations, routes);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ClearDemoDataAsync(CancellationToken cancellationToken)
    {
        db.ArrivalPredictions.RemoveRange(await db.ArrivalPredictions.ToListAsync(cancellationToken));
        db.RouteSegments.RemoveRange(await db.RouteSegments.ToListAsync(cancellationToken));
        db.RouteStations.RemoveRange(await db.RouteStations.ToListAsync(cancellationToken));
        db.BusPositions.RemoveRange(await db.BusPositions.ToListAsync(cancellationToken));
        db.Buses.RemoveRange(await db.Buses.ToListAsync(cancellationToken));
        db.Alerts.RemoveRange(await db.Alerts.ToListAsync(cancellationToken));
        db.Incidents.RemoveRange(await db.Incidents.ToListAsync(cancellationToken));
        db.OperationalRecommendations.RemoveRange(await db.OperationalRecommendations.ToListAsync(cancellationToken));
        db.Routes.RemoveRange(await db.Routes.ToListAsync(cancellationToken));
        db.Stations.RemoveRange(await db.Stations.ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<MetroSeed> LoadSeedAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Seed", "metro-seed.json");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<MetroSeed>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("No se pudo leer Seed/metro-seed.json.");
    }

    private static Station ToStation(SeedStation seed) =>
        new()
        {
            Code = seed.Id,
            Name = seed.Name,
            Sector = InferSector(seed.Name),
            Latitude = seed.Coordinates().Lat,
            Longitude = seed.Coordinates().Lng,
            OccupancyCurrent = seed.OccupancyCurrent,
            OccupancyPrediction20Min = seed.OccupancyPrediction20Min,
            Capacity = seed.Capacity,
            Recommendation = seed.Recommendation,
            CurrentOccupancyLevel = ParseOccupancy(seed.RiskLevel)
        };

    private static Route ToRoute(SeedRoute seed) =>
        new()
        {
            Code = seed.Id,
            Name = seed.Name,
            Description = $"{seed.Origin} hacia {seed.Destination}",
            Origin = seed.Origin,
            Destination = seed.Destination,
            ActiveBuses = seed.ActiveBuses,
            AvgTimeMinutes = seed.AvgTimeMinutes,
            DelayMinutes = seed.DelayMinutes,
            OccupancyLevel = ParseOccupancy(seed.Occupancy),
            FrontendStatus = seed.Status,
            PathCoordinatesJson = JsonSerializer.Serialize(seed.PathCoordinates),
            RouteType = seed.Id is "R1" or "R2" or "R3" ? RouteType.Trunk : RouteType.Feeder
        };

    private void AddRouteTopology(Route[] routes, Dictionary<string, Station> stationByName)
    {
        var routeStations = new Dictionary<string, string[]>
        {
            ["R1"] = ["Portal Cacique", "Portal Parque San Pio", "Portal Parque de los Ninos", "Portal Estadio Montanini"],
            ["R2"] = ["Portal Cacique", "Portal la Rosita", "Portal Parque de las Cigarras"],
            ["R3"] = ["Portal Cacique", "Portal la Rosita", "Portal Parque Santander", "Portal Parque de los Ninos", "Portal Estadio Montanini"],
            ["R4"] = ["Portal Cacique", "Portal Provenza"],
            ["R5"] = ["Portal Provenza", "Portal Canaveral"],
            ["R6"] = ["Portal Provenza", "Portal Quebrada Seca"]
        };

        foreach (var route in routes)
        {
            if (!routeStations.TryGetValue(route.Code, out var names)) continue;
            var stops = names.Select((name, index) => new RouteStation
            {
                RouteId = route.Id,
                StationId = stationByName[Normalize(name)].Id,
                StopOrder = index + 1,
                EstimatedMinutesFromStart = index * 6,
                IsTransferPoint = name.Contains("Provenza", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Cacique", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Rosita", StringComparison.OrdinalIgnoreCase)
            }).ToArray();
            db.RouteStations.AddRange(stops);
            for (var i = 0; i < stops.Length - 1; i++)
                db.RouteSegments.Add(new RouteSegment
                {
                    RouteId = route.Id,
                    OriginStationId = stops[i].StationId,
                    DestinationStationId = stops[i + 1].StationId,
                    DistanceKm = 1.1m + (i * 0.35m),
                    BaseTravelTimeMinutes = 5 + (i % 4)
                });
        }
    }

    private void AddBuses(IEnumerable<SeedBus> seeds, Dictionary<string, Route> routeByCode, Station[] stations)
    {
        foreach (var seed in seeds)
        {
            var route = routeByCode[seed.RouteId];
            var bus = new Bus
            {
                Plate = seed.Id,
                InternalCode = seed.Id,
                DriverName = seed.DriverName,
                Capacity = 90,
                CurrentOccupancy = OccupancyPercent(seed.Occupancy),
                OccupancyLevel = ParseOccupancy(seed.Occupancy),
                AssignedRouteId = route.Id,
                NextStation = seed.NextStation,
                EtaMinutes = seed.EtaMinutes,
                Status = seed.Status == "maintenance" ? BusStatus.Maintenance : seed.Status == "delayed" ? BusStatus.Delayed : BusStatus.InService
            };
            db.Buses.Add(bus);
            db.BusPositions.Add(new BusPosition
            {
                BusId = bus.Id,
                Latitude = seed.Latitude,
                Longitude = seed.Longitude,
                SpeedKmh = 24,
                CurrentStationId = stations.OrderBy(x => Math.Abs(x.Latitude - seed.Latitude) + Math.Abs(x.Longitude - seed.Longitude)).First().Id
            });
        }
    }

    private void AddAlerts(IEnumerable<SeedAlert> seeds, Dictionary<string, Route> routeByCode, Dictionary<string, Station> stationByName)
    {
        foreach (var seed in seeds)
        {
            var route = routeByCode.Values.FirstOrDefault(x => seed.Target.Contains(x.Code, StringComparison.OrdinalIgnoreCase));
            var station = stationByName.Values.FirstOrDefault(x => seed.Target.Contains(x.Name, StringComparison.OrdinalIgnoreCase));
            db.Alerts.Add(new Alert
            {
                ExternalCode = seed.Id,
                Title = seed.Type,
                Message = seed.Description,
                Target = seed.Target,
                Recommendation = seed.Recommendation,
                FrontendStatus = seed.Status,
                AlertType = seed.Level == "critical" ? AlertType.Critical : seed.Level == "warning" ? AlertType.Warning : AlertType.Info,
                Severity = ParseSeverity(seed.Level),
                RouteId = route?.Id,
                StationId = station?.Id,
                IsActive = seed.Status != "resolved",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            });
        }
    }

    private void AddIncidents(IEnumerable<SeedIncident> seeds, Dictionary<string, Route> routeByCode, Dictionary<string, Station> stationByName)
    {
        foreach (var seed in seeds)
        {
            var route = routeByCode.Values.FirstOrDefault(x => seed.AffectedRoute.Contains(x.Code, StringComparison.OrdinalIgnoreCase));
            var station = stationByName.Values.FirstOrDefault(x => seed.Location.Contains(x.Name.Replace("Portal ", ""), StringComparison.OrdinalIgnoreCase));
            db.Incidents.Add(new Incident
            {
                ExternalCode = seed.Id,
                Title = seed.Type,
                Description = $"{seed.Type} en {seed.Location}",
                Location = seed.Location,
                AffectedRoute = seed.AffectedRoute,
                OfficerInCharge = seed.OfficerInCharge,
                FrontendStatus = seed.Status,
                ActiveDurationMinutes = seed.ActiveDurationMinutes,
                Severity = seed.Status == "active" ? IncidentSeverity.High : IncidentSeverity.Medium,
                IncidentType = seed.Type.Contains("Falla", StringComparison.OrdinalIgnoreCase) ? IncidentType.BusFailure : IncidentType.RoadBlock,
                RouteId = route?.Id,
                StationId = station?.Id,
                IsActive = seed.Status != "resolved",
                StartedAt = DateTime.UtcNow.AddMinutes(-seed.ActiveDurationMinutes)
            });
        }
    }

    private void AddRecommendations(IEnumerable<SeedRecommendation> seeds, Dictionary<string, Route> routeByCode, Dictionary<string, Station> stationByCode)
    {
        foreach (var seed in seeds)
        {
            routeByCode.TryGetValue(seed.TargetId, out var route);
            stationByCode.TryGetValue(seed.TargetId, out var station);
            db.OperationalRecommendations.Add(new OperationalRecommendation
            {
                ExternalCode = seed.Id,
                Title = seed.Title,
                Description = seed.Suggestion,
                Impact = seed.Impact,
                FrontendType = seed.Type,
                TargetCode = seed.TargetId,
                RecommendationType = seed.Type switch
                {
                    "frequency" => RecommendationType.AdjustFrequency,
                    "route" => RecommendationType.Reroute,
                    "alert" => RecommendationType.TransferSuggestion,
                    _ => RecommendationType.DispatchBus
                },
                RouteId = route?.Id,
                StationId = station?.Id,
                Priority = seed.Priority switch { "critical" => 0, "high" => 1, "medium" => 2, _ => 3 },
                IsResolved = seed.Applied
            });
        }
    }

    private void AddArrivalPredictions(Station[] stations, Route[] routes)
    {
        foreach (var route in routes)
        foreach (var station in stations.Take(5))
            db.ArrivalPredictions.Add(new ArrivalPrediction
            {
                RouteId = route.Id,
                StationId = station.Id,
                EstimatedArrivalMinutes = Math.Max(3, route.AvgTimeMinutes / 4 + route.DelayMinutes),
                OccupancyLevel = route.OccupancyLevel,
                Confidence = 0.91m
            });
    }

    private static OccupancyLevel ParseOccupancy(string value) =>
        value.ToLowerInvariant() switch
        {
            "critical" => OccupancyLevel.Critical,
            "high" => OccupancyLevel.High,
            "medium" => OccupancyLevel.Medium,
            _ => OccupancyLevel.Low
        };

    private static IncidentSeverity ParseSeverity(string value) =>
        value.ToLowerInvariant() switch
        {
            "critical" => IncidentSeverity.Critical,
            "warning" => IncidentSeverity.High,
            "high" => IncidentSeverity.High,
            "medium" => IncidentSeverity.Medium,
            _ => IncidentSeverity.Low
        };

    private static int OccupancyPercent(string value) =>
        ParseOccupancy(value) switch
        {
            OccupancyLevel.Critical => 96,
            OccupancyLevel.High => 82,
            OccupancyLevel.Medium => 58,
            _ => 28
        };

    private static string InferSector(string stationName)
    {
        if (stationName.Contains("Canaveral", StringComparison.OrdinalIgnoreCase) || stationName.Contains("Provenza", StringComparison.OrdinalIgnoreCase)) return "Sur";
        if (stationName.Contains("Estadio", StringComparison.OrdinalIgnoreCase) || stationName.Contains("Quebrada", StringComparison.OrdinalIgnoreCase)) return "Norte";
        if (stationName.Contains("San Pio", StringComparison.OrdinalIgnoreCase) || stationName.Contains("Cacique", StringComparison.OrdinalIgnoreCase)) return "Oriente";
        return "Centro";
    }

    private static string Normalize(string value) =>
        value.Replace("Pio", "Pío", StringComparison.OrdinalIgnoreCase)
            .Replace("Ninos", "Niños", StringComparison.OrdinalIgnoreCase)
            .Replace("Canaveral", "Cañaveral", StringComparison.OrdinalIgnoreCase);

    private sealed record MetroSeed(
        SeedRoute[] Routes,
        SeedStation[] Stations,
        SeedBus[] Buses,
        SeedAlert[] Alerts,
        SeedIncident[] Incidents,
        SeedRecommendation[] Recommendations);

    private sealed record SeedRoute(string Id, string Name, string Origin, string Destination, int ActiveBuses, int AvgTimeMinutes, int DelayMinutes, string Occupancy, string Status, decimal[][] PathCoordinates);
    private sealed record SeedStation(string Id, string Name, int OccupancyCurrent, int OccupancyPrediction20Min, string RiskLevel, string Recommendation, int Capacity)
    {
        public (decimal Lat, decimal Lng) Coordinates() => Name switch
        {
            "Portal Parque de los Niños" => (7.1255442m, -73.1182953m),
            "Portal Parque San Pío" => (7.1184154m, -73.1113434m),
            "Portal Parque de las Cigarras" => (7.1031571m, -73.12144m),
            "Portal la Rosita" => (7.1126888m, -73.1221378m),
            "Portal Parque Santander" => (7.1194068m, -73.1227037m),
            "Portal Cacique" => (7.0994344m, -73.1067222m),
            "Portal Cañaveral" => (7.0707924m, -73.1054283m),
            "Portal Quebrada Seca" => (7.1223949m, -73.1282145m),
            "Portal Provenza" => (7.0883168m, -73.1084067m),
            "Portal Estadio Montanini" => (7.1364354m, -73.1178984m),
            _ => (7.1085m, -73.1180m)
        };
    }
    private sealed record SeedBus(string Id, string RouteId, string DriverName, decimal Latitude, decimal Longitude, string Occupancy, string Status, string NextStation, int EtaMinutes);
    private sealed record SeedAlert(string Id, string Type, string Target, string Level, string Description, string Recommendation, string Timestamp, string Status);
    private sealed record SeedIncident(string Id, string Type, string Location, string AffectedRoute, string Status, int ActiveDurationMinutes, string OfficerInCharge);
    private sealed record SeedRecommendation(string Id, string Title, string Impact, string Priority, string Suggestion, bool Applied, string Type, string TargetId);
}
