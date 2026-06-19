using System.Net.Http.Json;
using Application.Common;
using Application.VectorSearch.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.VectorSearch.Chroma;

public sealed class ChromaVectorIndexingService(HttpClient http, IConfiguration config, IMetroFlowDbContext db) : IVectorIndexingService
{
    public async Task<int> ReindexAsync(CancellationToken cancellationToken = default)
    {
        var docs = new List<object>();
        docs.AddRange(await db.Stations.AsNoTracking().Select(x => new
        {
            id = $"station:{x.Id}",
            document = $"Estacion {x.Name} ubicada en {x.Sector}. Codigo {x.Code}. Ocupacion {x.CurrentOccupancyLevel}.",
            metadata = new { entityType = "station", entityId = x.Id, x.Code, x.Name, x.Sector }
        }).ToListAsync(cancellationToken));
        docs.AddRange(await db.Routes.AsNoTracking().Select(x => new
        {
            id = $"route:{x.Id}",
            document = $"Ruta {x.Code} {x.Name}. {x.Description}. Tipo {x.RouteType}.",
            metadata = new { entityType = "route", entityId = x.Id, x.Code, x.Name }
        }).ToListAsync(cancellationToken));
        docs.AddRange(await db.Incidents.AsNoTracking().Select(x => new
        {
            id = $"incident:{x.Id}",
            document = $"Incidente {x.Title}. {x.Description}. Severidad {x.Severity}. Tipo {x.IncidentType}.",
            metadata = new { entityType = "incident", entityId = x.Id, x.Title }
        }).ToListAsync(cancellationToken));
        docs.AddRange(await db.OperationalRecommendations.AsNoTracking().Select(x => new
        {
            id = $"recommendation:{x.Id}",
            document = $"Recomendacion {x.Title}. {x.Description}. Prioridad {x.Priority}.",
            metadata = new { entityType = "recommendation", entityId = x.Id, x.Title }
        }).ToListAsync(cancellationToken));

        try
        {
            var collection = config["Chroma:CollectionName"] ?? "metroflow_knowledge";
            await http.PostAsJsonAsync("/api/v1/collections", new { name = collection }, cancellationToken);
            await http.PostAsJsonAsync($"/api/v1/collections/{collection}/add", new
            {
                ids = docs.Select(x => x.GetType().GetProperty("id")!.GetValue(x)).ToArray(),
                documents = docs.Select(x => x.GetType().GetProperty("document")!.GetValue(x)).ToArray(),
                metadatas = docs.Select(x => x.GetType().GetProperty("metadata")!.GetValue(x)).ToArray()
            }, cancellationToken);
        }
        catch
        {
            // Indexing is best-effort; MySQL remains source of truth and search has fallback.
        }

        return docs.Count;
    }
}

