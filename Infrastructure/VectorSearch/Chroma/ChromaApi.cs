using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.VectorSearch.Chroma;

internal static partial class ChromaApi
{
    private const string Tenant = "default_tenant";
    private const string Database = "default_database";
    private const int Dimensions = 32;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<string> GetOrCreateCollectionIdAsync(HttpClient http, string collectionName, CancellationToken cancellationToken)
    {
        var collection = await FindCollectionAsync(http, collectionName, cancellationToken);
        if (collection is not null) return collection.Id;

        var response = await http.PostAsJsonAsync(CollectionsPath, new { name = collectionName }, cancellationToken);
        response.EnsureSuccessStatusCode();

        collection = await response.Content.ReadFromJsonAsync<ChromaCollection>(JsonOptions, cancellationToken);
        if (collection is null || string.IsNullOrWhiteSpace(collection.Id))
            throw new InvalidOperationException("ChromaDB did not return a collection id.");

        return collection.Id;
    }

    public static async Task ClearCollectionIfExistsAsync(HttpClient http, string collectionName, CancellationToken cancellationToken)
    {
        var collection = await FindCollectionAsync(http, collectionName, cancellationToken);
        if (collection is null) return;

        var existing = await http.PostAsJsonAsync($"{CollectionPath(collection.Id)}/get", new { limit = 10_000 }, cancellationToken);
        existing.EnsureSuccessStatusCode();

        var payload = await existing.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
        if (!payload.TryGetProperty("ids", out var idsElement) || idsElement.GetArrayLength() == 0) return;

        var ids = idsElement.EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
        if (ids.Length == 0) return;

        var response = await http.PostAsJsonAsync($"{CollectionPath(collection.Id)}/delete", new { ids }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<string?> TryGetCollectionIdAsync(HttpClient http, string collectionName, CancellationToken cancellationToken)
    {
        var collection = await FindCollectionAsync(http, collectionName, cancellationToken);
        return collection?.Id;
    }

    public static string CollectionPath(string collectionId) => $"{CollectionsPath}/{collectionId}";

    public static float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (Match match in TokenRegex().Matches(text.ToLowerInvariant()))
        {
            var hash = StableHash(match.Value);
            var index = Math.Abs(hash % Dimensions);
            vector[index] += 1f;
        }

        var norm = Math.Sqrt(vector.Sum(x => x * x));
        if (norm <= 0) return vector;

        for (var i = 0; i < vector.Length; i++)
            vector[i] = (float)(vector[i] / norm);

        return vector;
    }

    public static Dictionary<string, string> ToStringDictionary(JsonElement element)
    {
        var result = new Dictionary<string, string>();
        if (element.ValueKind != JsonValueKind.Object) return result;

        foreach (var property in element.EnumerateObject())
            result[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? ""
                : property.Value.ToString();

        return result;
    }

    private static async Task<ChromaCollection?> FindCollectionAsync(HttpClient http, string collectionName, CancellationToken cancellationToken)
    {
        var collections = await http.GetFromJsonAsync<List<ChromaCollection>>(CollectionsPath, JsonOptions, cancellationToken) ?? [];
        return collections.FirstOrDefault(x => string.Equals(x.Name, collectionName, StringComparison.Ordinal));
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in value)
                hash = (hash * 31) + c;
            return hash;
        }
    }

    private static string CollectionsPath => $"/api/v2/tenants/{Tenant}/databases/{Database}/collections";

    [GeneratedRegex("[\\p{L}\\p{N}]+")]
    private static partial Regex TokenRegex();

    private sealed record ChromaCollection(string Id, string Name);
}
