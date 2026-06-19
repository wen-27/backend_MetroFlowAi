using Application.VectorSearch.Models;

namespace Application.VectorSearch.Abstractions;

public interface IVectorSearchService
{
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string query, int limit = 5, CancellationToken cancellationToken = default);
}

