using System.Net.Http.Json;
using System.Text.Json;
using Application.Common;
using Application.VectorSearch.Abstractions;
using Application.VectorSearch.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.VectorSearch.Chroma;

public sealed class ChromaVectorSearchService(HttpClient http, IConfiguration config, IMetroFlowDbContext db) : IVectorSearchService
{
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        try
        {
            var collectionName = config["Chroma:CollectionName"] ?? "metroflow_knowledge";
            var collectionId = await ChromaApi.TryGetCollectionIdAsync(http, collectionName, cancellationToken);
            if (!string.IsNullOrWhiteSpace(collectionId))
            {
                var response = await http.PostAsJsonAsync($"{ChromaApi.CollectionPath(collectionId)}/query", new
                {
                    query_embeddings = new[] { ChromaApi.Embed(query) },
                    n_results = limit,
                    include = new[] { "documents", "metadatas", "distances" }
                }, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                    var results = MapResults(payload);
                    if (results.Count > 0) return results;
                }
            }
        }
        catch
        {
            // Fallback below keeps the demo useful when ChromaDB is not running.
        }

        var q = query.ToLowerInvariant();
        var stations = await db.Stations.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(q) || x.Code.ToLower().Contains(q) || x.Sector.ToLower().Contains(q))
            .Take(limit)
            .ToListAsync(cancellationToken);
        var stationMatches = stations.Select(x => new VectorSearchResult($"station:{x.Id}", $"Estacion {x.Name} en sector {x.Sector}. Codigo {x.Code}.", 0.65, new Dictionary<string, string>
        {
            ["entityType"] = "station",
            ["entityId"] = x.Id.ToString(),
            ["name"] = x.Name,
            ["code"] = x.Code,
            ["fallback"] = "postgres"
        })).ToList();

        if (stationMatches.Count > 0) return stationMatches;

        return [new VectorSearchResult("fallback:none", "ChromaDB no disponible y no hubo coincidencias simples en PostgreSQL.", 1, new Dictionary<string, string> { ["fallback"] = "postgres" })];
    }

    private static IReadOnlyList<VectorSearchResult> MapResults(JsonElement payload)
    {
        var results = new List<VectorSearchResult>();
        if (!payload.TryGetProperty("ids", out var ids) || ids.GetArrayLength() == 0) return results;

        var firstIds = ids[0];
        var documents = payload.TryGetProperty("documents", out var docs) && docs.GetArrayLength() > 0 ? docs[0] : default;
        var metadatas = payload.TryGetProperty("metadatas", out var metas) && metas.GetArrayLength() > 0 ? metas[0] : default;
        var distances = payload.TryGetProperty("distances", out var dists) && dists.GetArrayLength() > 0 ? dists[0] : default;

        for (var i = 0; i < firstIds.GetArrayLength(); i++)
        {
            var id = firstIds[i].GetString() ?? "";
            var content = documents.ValueKind == JsonValueKind.Array && documents.GetArrayLength() > i
                ? documents[i].GetString() ?? ""
                : "";
            var score = distances.ValueKind == JsonValueKind.Array && distances.GetArrayLength() > i
                ? distances[i].GetDouble()
                : 0;
            var metadata = metadatas.ValueKind == JsonValueKind.Array && metadatas.GetArrayLength() > i
                ? ChromaApi.ToStringDictionary(metadatas[i])
                : new Dictionary<string, string>();
            metadata["source"] = "chroma";
            results.Add(new VectorSearchResult(id, content, score, metadata));
        }

        return results;
    }
}
