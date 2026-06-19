using Application.Common;
using Application.Routing.Abstractions;
using Application.VectorSearch.Abstractions;
using Infrastructure.Persistence;
using Infrastructure.Routing.Graph;
using Infrastructure.Seed;
using Infrastructure.VectorSearch.Chroma;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=metroflow_ai;Username=metroflow;Password=metroflow123";

        services.AddDbContext<MetroFlowDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgres => postgres.EnableRetryOnFailure()));
        services.AddScoped<IMetroFlowDbContext>(sp => sp.GetRequiredService<MetroFlowDbContext>());
        services.AddScoped<MetroFlowSeeder>();
        services.AddMemoryCache();
        services.AddScoped<IRouteGraphService, RouteGraphService>();
        services.AddHttpClient<IVectorSearchService, ChromaVectorSearchService>(client =>
            client.BaseAddress = new Uri(configuration["Chroma:BaseUrl"] ?? "http://localhost:8001"));
        services.AddHttpClient<IVectorIndexingService, ChromaVectorIndexingService>(client =>
            client.BaseAddress = new Uri(configuration["Chroma:BaseUrl"] ?? "http://localhost:8001"));
        return services;
    }
}
