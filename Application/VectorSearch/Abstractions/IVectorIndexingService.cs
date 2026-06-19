namespace Application.VectorSearch.Abstractions;

public interface IVectorIndexingService
{
    Task<int> ReindexAsync(CancellationToken cancellationToken = default);
}

