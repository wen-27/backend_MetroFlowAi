using Domain.Alerts.Entities;
using Domain.Buses.Entities;
using Domain.BusPositions.Entities;
using Domain.Incidents.Entities;
using Domain.Predictions.Entities;
using Domain.Recommendations.Entities;
using Domain.Routes.Entities;
using Domain.RouteSegments.Entities;
using Domain.RouteStations.Entities;
using Domain.Stations.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common;

public interface IMetroFlowDbContext
{
    DbSet<Station> Stations { get; }
    DbSet<Route> Routes { get; }
    DbSet<RouteStation> RouteStations { get; }
    DbSet<RouteSegment> RouteSegments { get; }
    DbSet<Bus> Buses { get; }
    DbSet<BusPosition> BusPositions { get; }
    DbSet<Incident> Incidents { get; }
    DbSet<Alert> Alerts { get; }
    DbSet<ArrivalPrediction> ArrivalPredictions { get; }
    DbSet<OperationalRecommendation> OperationalRecommendations { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
