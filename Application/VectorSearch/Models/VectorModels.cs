namespace Application.VectorSearch.Models;

public sealed record VectorDocument(string Id, string Content, IReadOnlyDictionary<string, string> Metadata);
public sealed record VectorSearchResult(string Id, string Content, double Score, IReadOnlyDictionary<string, string> Metadata);

