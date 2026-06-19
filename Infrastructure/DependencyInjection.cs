using Application.Assistant.Abstractions;
using Application.Common;
using Application.Routing.Abstractions;
using Application.VectorSearch.Abstractions;
using Infrastructure.Assistant;
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
            ?? "server=localhost;port=3306;database=metroflow_ai;user=metroflow;password=metroflow123";

        services.AddDbContext<MetroFlowDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
        services.AddScoped<IMetroFlowDbContext>(sp => sp.GetRequiredService<MetroFlowDbContext>());
        services.AddScoped<MetroFlowSeeder>();
        services.AddMemoryCache();
        services.AddScoped<IRouteGraphService, RouteGraphService>();
        services.AddScoped<IAssistantService, AssistantService>();
        services.AddHttpClient<IVectorSearchService, ChromaVectorSearchService>(client =>
            client.BaseAddress = new Uri(configuration["Chroma:BaseUrl"] ?? "http://localhost:8001"));
        services.AddHttpClient<IVectorIndexingService, ChromaVectorIndexingService>(client =>
            client.BaseAddress = new Uri(configuration["Chroma:BaseUrl"] ?? "http://localhost:8001"));
        return services;
    }
}
