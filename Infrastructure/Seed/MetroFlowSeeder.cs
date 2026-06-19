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
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Stations.AnyAsync(cancellationToken)) return;

        var stations = new[]
        {
            S("ST-UIS", "UIS", "Norte", 7.140m, -73.121m, OccupancyLevel.High),
            S("ST-QSE", "Quebrada Seca", "Centro", 7.126m, -73.123m, OccupancyLevel.Medium),
            S("ST-CEN", "Centro", "Centro", 7.119m, -73.122m, OccupancyLevel.High),
            S("ST-PSA", "Parque Santander", "Centro", 7.117m, -73.121m, OccupancyLevel.Medium),
            S("ST-LRO", "La Rosita", "Centro", 7.113m, -73.121m, OccupancyLevel.Medium),
            S("ST-PSO", "Puerta del Sol", "Occidente", 7.104m, -73.117m, OccupancyLevel.High),
            S("ST-CAB", "Cabecera", "Oriente", 7.118m, -73.108m, OccupancyLevel.Medium),
            S("ST-SPI", "San Pio", "Cabecera", 7.115m, -73.107m, OccupancyLevel.Low),
            S("ST-MEG", "Megamall", "Cabecera", 7.121m, -73.109m, OccupancyLevel.Medium),
            S("ST-PRO", "Provenza", "Sur", 7.087m, -73.105m, OccupancyLevel.High),
            S("ST-LCA", "Lagos del Cacique", "Sur", 7.095m, -73.105m, OccupancyLevel.Low),
            S("ST-RMI", "Real de Minas", "Occidente", 7.109m, -73.125m, OccupancyLevel.Medium),
            S("ST-CIU", "Ciudadela", "Occidente", 7.107m, -73.128m, OccupancyLevel.Low),
            S("ST-MUT", "Mutis", "Occidente", 7.113m, -73.132m, OccupancyLevel.Low),
            S("ST-GIR", "Girardot", "Norte", 7.130m, -73.131m, OccupancyLevel.Medium),
            S("ST-CON", "La Concordia", "Centro", 7.112m, -73.119m, OccupancyLevel.Medium),
            S("ST-DIA", "Diamante", "Sur", 7.083m, -73.110m, OccupancyLevel.Low),
            S("ST-MOR", "Morrorico", "Oriente", 7.129m, -73.102m, OccupancyLevel.Low),
            S("ST-CMA", "Cafe Madrid", "Norte", 7.156m, -73.135m, OccupancyLevel.Medium),
            S("ST-NOR", "Norte", "Norte", 7.149m, -73.128m, OccupancyLevel.Medium),
        };
        db.Stations.AddRange(stations);

        var routes = new[]
        {
            R("MF-01", "Centro - UIS - Norte", RouteType.Trunk),
            R("MF-02", "Provenza - Centro - UIS", RouteType.Trunk),
            R("MF-03", "Real de Minas - Cabecera - Centro", RouteType.Feeder),
            R("MF-04", "Cafe Madrid - Centro - Puerta del Sol", RouteType.Express),
            R("MF-05", "Diamante - Provenza - Cabecera", RouteType.Feeder),
            R("MF-06", "Morrorico - Centro - San Pio", RouteType.Feeder),
            R("MF-07", "Mutis - Real de Minas - Centro", RouteType.Feeder),
            R("MF-08", "Circular Centro - Cabecera - Provenza", RouteType.Circular),
        };
        db.Routes.AddRange(routes);
        await db.SaveChangesAsync(cancellationToken);

        AddRoute(routes[0], stations, ["Centro", "Quebrada Seca", "UIS", "Norte"]);
        AddRoute(routes[1], stations, ["Provenza", "Puerta del Sol", "La Rosita", "Centro", "Quebrada Seca", "UIS"]);
        AddRoute(routes[2], stations, ["Real de Minas", "Puerta del Sol", "Cabecera", "San Pio", "Centro"]);
        AddRoute(routes[3], stations, ["Cafe Madrid", "Girardot", "Centro", "La Rosita", "Puerta del Sol"]);
        AddRoute(routes[4], stations, ["Diamante", "Provenza", "Lagos del Cacique", "Cabecera"]);
        AddRoute(routes[5], stations, ["Morrorico", "Megamall", "Centro", "San Pio"]);
        AddRoute(routes[6], stations, ["Mutis", "Ciudadela", "Real de Minas", "La Concordia", "Centro"]);
        AddRoute(routes[7], stations, ["Centro", "Cabecera", "Provenza", "Diamante", "Puerta del Sol", "Centro"]);

        for (var i = 1; i <= 18; i++)
        {
            var route = routes[(i - 1) % routes.Length];
            var station = stations[i % stations.Length];
            var bus = new Bus
            {
                Plate = $"MFA{i:000}",
                InternalCode = $"MF-BUS-{i:000}",
                BusType = i % 4 == 0 ? BusType.Electric : i % 3 == 0 ? BusType.Feeder : BusType.Standard,
                Capacity = i % 3 == 0 ? 60 : 90,
                CurrentOccupancy = 20 + (i * 7 % 70),
                OccupancyLevel = i % 5 == 0 ? OccupancyLevel.High : i % 2 == 0 ? OccupancyLevel.Medium : OccupancyLevel.Low,
                AssignedRouteId = route.Id,
                Status = i % 7 == 0 ? BusStatus.Delayed : i == 12 ? BusStatus.OutOfService : BusStatus.InService
            };
            db.Buses.Add(bus);
            db.BusPositions.Add(new BusPosition { BusId = bus.Id, Latitude = station.Latitude, Longitude = station.Longitude, SpeedKmh = 24, CurrentStationId = station.Id });
        }

        var byName = stations.ToDictionary(x => x.Name);
        var byCode = routes.ToDictionary(x => x.Code);
        db.Incidents.AddRange(
            I("Congestion alta en Provenza", IncidentSeverity.High, IncidentType.Congestion, byCode["MF-02"].Id, byName["Provenza"].Id),
            I("Retraso en ruta MF-02", IncidentSeverity.Medium, IncidentType.Delay, byCode["MF-02"].Id, null),
            I("Alto flujo en UIS", IncidentSeverity.Medium, IncidentType.Operational, null, byName["UIS"].Id),
            I("Bus fuera de servicio en Centro", IncidentSeverity.High, IncidentType.BusFailure, null, byName["Centro"].Id),
            I("Congestion media en Puerta del Sol", IncidentSeverity.Medium, IncidentType.Congestion, byCode["MF-04"].Id, byName["Puerta del Sol"].Id));
        db.Alerts.AddRange(
            A("Alta ocupacion en Provenza", "La demanda supera el promedio del corredor sur.", byCode["MF-02"].Id, byName["Provenza"].Id),
            A("Retrasos hacia el Centro", "Se reportan demoras de 8 a 12 minutos.", byCode["MF-02"].Id, byName["Centro"].Id),
            A("Ruta alternativa recomendada por congestion", "Considere transbordo por Cabecera.", byCode["MF-08"].Id, null),
            A("Mayor demanda cerca de UIS", "Refuerzo recomendado en hora pico.", null, byName["UIS"].Id));
        db.OperationalRecommendations.AddRange(
            Rec("Enviar bus adicional a Provenza", RecommendationType.DispatchBus, byCode["MF-02"].Id, byName["Provenza"].Id, 1),
            Rec("Ajustar frecuencia en MF-02", RecommendationType.AdjustFrequency, byCode["MF-02"].Id, null, 2),
            Rec("Recomendar transbordo por Cabecera", RecommendationType.TransferSuggestion, byCode["MF-08"].Id, byName["Cabecera"].Id, 2),
            Rec("Desviar flujo hacia MF-08", RecommendationType.Reroute, byCode["MF-08"].Id, null, 3));

        foreach (var route in routes.Take(4))
        foreach (var station in stations.Take(6))
            db.ArrivalPredictions.Add(new ArrivalPrediction
            {
                RouteId = route.Id,
                StationId = station.Id,
                EstimatedArrivalMinutes = new[] { 4, 7, 12, 18 }[(route.Code[^1] + station.Code[^1]) % 4],
                OccupancyLevel = station.CurrentOccupancyLevel,
                Confidence = 0.86m
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Station S(string code, string name, string sector, decimal lat, decimal lng, OccupancyLevel occupancy) =>
        new() { Code = code, Name = name, Sector = sector, Latitude = lat, Longitude = lng, CurrentOccupancyLevel = occupancy };

    private static Route R(string code, string name, RouteType type) =>
        new() { Code = code, Name = name, Description = $"Ruta sintetica MetroFlow {name}", RouteType = type };

    private static Incident I(string title, IncidentSeverity severity, IncidentType type, Guid? routeId, Guid? stationId) =>
        new() { Title = title, Description = title, Severity = severity, IncidentType = type, RouteId = routeId, StationId = stationId };

    private static Alert A(string title, string message, Guid? routeId, Guid? stationId) =>
        new() { Title = title, Message = message, AlertType = AlertType.Warning, Severity = IncidentSeverity.Medium, RouteId = routeId, StationId = stationId };

    private static OperationalRecommendation Rec(string title, RecommendationType type, Guid? routeId, Guid? stationId, int priority) =>
        new() { Title = title, Description = title, RecommendationType = type, RouteId = routeId, StationId = stationId, Priority = priority };

    private void AddRoute(Route route, Station[] stations, string[] names)
    {
        var routeStations = names.Select((name, index) => new RouteStation
        {
            RouteId = route.Id,
            StationId = stations.Single(x => x.Name == name).Id,
            StopOrder = index + 1,
            EstimatedMinutesFromStart = index * 6,
            IsTransferPoint = name is "Centro" or "Cabecera" or "Puerta del Sol"
        }).ToArray();
        db.RouteStations.AddRange(routeStations);
        for (var i = 0; i < routeStations.Length - 1; i++)
            db.RouteSegments.Add(new RouteSegment
            {
                RouteId = route.Id,
                OriginStationId = routeStations[i].StationId,
                DestinationStationId = routeStations[i + 1].StationId,
                DistanceKm = 1.2m + (i * 0.25m),
                BaseTravelTimeMinutes = 5 + (i % 3)
            });
    }
}
