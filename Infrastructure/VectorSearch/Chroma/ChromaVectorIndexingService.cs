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
        var docs = new List<ChromaDocument>();
        var stations = await db.Stations.AsNoTracking().Select(x => new
        {
            id = $"station:{x.Id}",
            document = $"Estacion {x.Name} ubicada en {x.Sector}. Codigo {x.Code}. Ocupacion {x.CurrentOccupancyLevel}.",
            x.Id,
            x.Code,
            x.Name,
            x.Sector
        }).ToListAsync(cancellationToken);
        docs.AddRange(stations.Select(x => new ChromaDocument(x.id, x.document, new Dictionary<string, object>
        {
            ["entityType"] = "station",
            ["entityId"] = x.Id.ToString(),
            ["code"] = x.Code,
            ["name"] = x.Name,
            ["sector"] = x.Sector
        })));

        var routes = await db.Routes.AsNoTracking().Select(x => new
        {
            id = $"route:{x.Id}",
            document = $"Ruta {x.Code} {x.Name}. {x.Description}. Tipo {x.RouteType}.",
            x.Id,
            x.Code,
            x.Name
        }).ToListAsync(cancellationToken);
        docs.AddRange(routes.Select(x => new ChromaDocument(x.id, x.document, new Dictionary<string, object>
        {
            ["entityType"] = "route",
            ["entityId"] = x.Id.ToString(),
            ["code"] = x.Code,
            ["name"] = x.Name
        })));

        var incidents = await db.Incidents.AsNoTracking().Select(x => new
        {
            id = $"incident:{x.Id}",
            document = $"Incidente {x.Title}. {x.Description}. Severidad {x.Severity}. Tipo {x.IncidentType}.",
            x.Id,
            x.Title
        }).ToListAsync(cancellationToken);
        docs.AddRange(incidents.Select(x => new ChromaDocument(x.id, x.document, new Dictionary<string, object>
        {
            ["entityType"] = "incident",
            ["entityId"] = x.Id.ToString(),
            ["title"] = x.Title
        })));

        var recommendations = await db.OperationalRecommendations.AsNoTracking().Select(x => new
        {
            id = $"recommendation:{x.Id}",
            document = $"Recomendacion {x.Title}. {x.Description}. Prioridad {x.Priority}.",
            x.Id,
            x.Title
        }).ToListAsync(cancellationToken);
        docs.AddRange(recommendations.Select(x => new ChromaDocument(x.id, x.document, new Dictionary<string, object>
        {
            ["entityType"] = "recommendation",
            ["entityId"] = x.Id.ToString(),
            ["title"] = x.Title
        })));

        var collectionName = config["Chroma:CollectionName"] ?? "metroflow_knowledge";
        await ChromaApi.ClearCollectionIfExistsAsync(http, collectionName, cancellationToken);
        var collectionId = await ChromaApi.GetOrCreateCollectionIdAsync(http, collectionName, cancellationToken);
        var response = await http.PostAsJsonAsync($"{ChromaApi.CollectionPath(collectionId)}/upsert", new
        {
            ids = docs.Select(x => x.Id).ToArray(),
            documents = docs.Select(x => x.Document).ToArray(),
            embeddings = docs.Select(x => ChromaApi.Embed(x.Document)).ToArray(),
            metadatas = docs.Select(x => x.Metadata).ToArray()
        }, cancellationToken);
        response.EnsureSuccessStatusCode();

        return docs.Count;
    }

    private sealed record ChromaDocument(string Id, string Document, Dictionary<string, object> Metadata);
}
