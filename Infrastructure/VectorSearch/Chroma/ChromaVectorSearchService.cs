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
            var collection = config["Chroma:CollectionName"] ?? "metroflow_knowledge";
            var response = await http.PostAsJsonAsync($"/api/v1/collections/{collection}/query", new
            {
                query_texts = new[] { query },
                n_results = limit
            }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return [new VectorSearchResult("chroma:raw", json, 0.1, new Dictionary<string, string> { ["source"] = "chroma" })];
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
            ["fallback"] = "mysql"
        })).ToList();

        if (stationMatches.Count > 0) return stationMatches;

        return [new VectorSearchResult("fallback:none", "ChromaDB no disponible y no hubo coincidencias simples en MySQL.", 1, new Dictionary<string, string> { ["fallback"] = "mysql" })];
    }
}
