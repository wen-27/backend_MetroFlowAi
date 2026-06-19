using Application.Common;
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

namespace Infrastructure.Persistence;

public sealed class MetroFlowDbContext(DbContextOptions<MetroFlowDbContext> options) : DbContext(options), IMetroFlowDbContext
{
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStation> RouteStations => Set<RouteStation>();
    public DbSet<RouteSegment> RouteSegments => Set<RouteSegment>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<BusPosition> BusPositions => Set<BusPosition>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<ArrivalPrediction> ArrivalPredictions => Set<ArrivalPrediction>();
    public DbSet<OperationalRecommendation> OperationalRecommendations => Set<OperationalRecommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MetroFlowDbContext).Assembly);
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            entity.SetTableName(entity.ClrType.Name);

        modelBuilder.Entity<Station>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Route>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Bus>().HasIndex(x => x.InternalCode).IsUnique();
    }
}
