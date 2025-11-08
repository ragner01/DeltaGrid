using IOC.Search.Models;

namespace IOC.Search.Querying;

/// <summary>
/// Search service with security trimming by tenant/role
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Search documents with filters and security trimming
    /// </summary>
    Task<SearchResponse> SearchAsync(SearchQuery query, CancellationToken ct = default);

    /// <summary>
    /// Q&A endpoint: semantic search + LLM for answer generation
    /// </summary>
    Task<QaResponse> AskOpsAsync(QaRequest request, CancellationToken ct = default);

    /// <summary>
    /// Record search feedback for relevancy tuning
    /// </summary>
    Task RecordFeedbackAsync(SearchFeedback feedback, CancellationToken ct = default);
}

/// <summary>
/// Search response with results and facets
/// </summary>
public sealed class SearchResponse
{
    public List<SearchResult> Results { get; init; } = new();
    public int TotalCount { get; init; }
    public Dictionary<string, List<Facet>> Facets { get; init; } = new();
    public TimeSpan ElapsedMs { get; init; }
}

public sealed record Facet(string Value, int Count);

