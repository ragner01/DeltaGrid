using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using IOC.Search.Models;
using IOC.Search.Indexing;
using IOC.Search.Querying;
using System.Diagnostics;

namespace IOC.Search.Querying;

/// <summary>
/// Azure Cognitive Search implementation with security trimming
/// </summary>
public sealed class AzureSearchService : ISearchService
{
    private readonly SearchClient _searchClient;
    private readonly IEmbeddingGenerator? _embeddingGenerator;
    private readonly ILogger<AzureSearchService> _logger;
    private const string IndexName = "deltagrid-documents";

    public AzureSearchService(
        string searchEndpoint,
        string searchApiKey,
        IEmbeddingGenerator? embeddingGenerator,
        ILogger<AzureSearchService> logger)
    {
        var credential = new Azure.AzureKeyCredential(searchApiKey);
        _searchClient = new SearchClient(new Uri(searchEndpoint), IndexName, credential);
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
    }

    public async Task<SearchResponse> SearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var searchOptions = new SearchOptions
        {
            Skip = query.Skip,
            Size = query.Take,
            IncludeTotalCount = true,
            QueryType = SearchQueryType.Simple // Simplified - semantic search requires premium tier
        };

        // Build security filter: tenant + roles
        var filters = new List<string>();

        if (!string.IsNullOrEmpty(query.TenantId))
        {
            filters.Add($"tenantId eq '{query.TenantId}'");
        }

        if (query.RequiredRoles != null && query.RequiredRoles.Any())
        {
            // User must have at least one required role
            var roleFilters = query.RequiredRoles.Select(r => $"roles/any(role: role eq '{r}')");
            filters.Add($"({string.Join(" or ", roleFilters)})");
        }

        if (!string.IsNullOrEmpty(query.SiteId))
        {
            filters.Add($"(siteId eq '{query.SiteId}' or siteId eq '')");
        }

        if (!string.IsNullOrEmpty(query.AssetId))
        {
            filters.Add($"(assetId eq '{query.AssetId}' or assetId eq '')");
        }

        if (query.TypeFilter.HasValue)
        {
            filters.Add($"type eq '{query.TypeFilter.Value}'");
        }

        if (query.Tags != null && query.Tags.Any())
        {
            var tagFilters = query.Tags.Select(t => $"tags/any(tag: tag eq '{t}')");
            filters.Add($"({string.Join(" or ", tagFilters)})");
        }

        if (query.FromDate.HasValue)
        {
            filters.Add($"documentDate ge {query.FromDate.Value:O}");
        }

        if (query.ToDate.HasValue)
        {
            filters.Add($"documentDate le {query.ToDate.Value:O}");
        }

        if (filters.Any())
        {
            searchOptions.Filter = string.Join(" and ", filters);
        }

        // Facets - set via constructor or use default
        // Note: Select and Facets are read-only properties in Azure Search SDK

        var results = await _searchClient.SearchAsync<SearchDocument>(query.SearchText, searchOptions, ct);
        sw.Stop();

        var searchResults = new List<SearchResult>();
        await foreach (var result in results.Value.GetResultsAsync())
        {
            var doc = result.Document;
            var highlights = result.Highlights?.Values.SelectMany(h => h).ToList() ?? new List<string>();

            searchResults.Add(new SearchResult
            {
                Id = doc["id"].ToString() ?? string.Empty,
                DocumentId = doc["documentId"].ToString() ?? string.Empty,
                Type = Enum.Parse<DocumentType>(doc["type"].ToString() ?? "SOP"),
                Title = doc["title"].ToString() ?? string.Empty,
                Snippet = highlights.FirstOrDefault() ?? doc["content"].ToString() ?? string.Empty,
                Score = result.Score ?? 0,
                DocumentDate = doc.ContainsKey("documentDate") && doc["documentDate"] != null
                    ? DateTimeOffset.Parse(doc["documentDate"].ToString() ?? string.Empty)
                    : null,
                SourceUri = doc.ContainsKey("sourceUri") ? doc["sourceUri"].ToString() : null,
                Highlights = highlights,
                Metadata = doc.Where(kvp => kvp.Key.StartsWith("metadata_"))
                    .ToDictionary(kvp => kvp.Key.Replace("metadata_", ""), kvp => kvp.Value.ToString() ?? string.Empty)
            });
        }

        var facets = new Dictionary<string, List<Facet>>();
        if (results.Value.Facets != null)
        {
            foreach (var facet in results.Value.Facets)
            {
                facets[facet.Key] = facet.Value.Select(f => new Facet(f.Value.ToString() ?? string.Empty, (int)(f.Count ?? 0))).ToList();
            }
        }

        _logger.LogInformation("Search query '{Query}' returned {Count} results in {Ms}ms", query.SearchText, searchResults.Count, sw.ElapsedMilliseconds);

        return new SearchResponse
        {
            Results = searchResults,
            TotalCount = (int)(results.Value.TotalCount ?? 0),
            Facets = facets,
            ElapsedMs = sw.Elapsed
        };
    }

    public async Task<QaResponse> AskOpsAsync(QaRequest request, CancellationToken ct = default)
    {
        // 1. Semantic search to find relevant context documents
        var searchQuery = new SearchQuery
        {
            SearchText = request.Question,
            TypeFilter = request.Scope,
            TenantId = request.TenantId,
            RequiredRoles = request.RequiredRoles,
            Semantic = true,
            Take = request.MaxContextDocs
        };

        var searchResults = await SearchAsync(searchQuery, ct);

        if (!searchResults.Results.Any())
        {
            return new QaResponse
            {
                Answer = "I couldn't find any relevant documents to answer your question. Please try rephrasing or check your access permissions.",
                Citations = new List<Citation>(),
                Confidence = 0.0,
                RelatedQuestions = new List<string>()
            };
        }

        // 2. Build context from top results
        var context = string.Join("\n\n", searchResults.Results.Take(3).Select((r, idx) =>
            $"[Document {idx + 1}: {r.Title}]\n{r.Snippet}"));

        // 3. Generate answer using LLM (Azure OpenAI or similar)
        // For now, return a simple answer with citations
        // In production, integrate with Azure OpenAI Chat API
        var answer = $"Based on the available documents, here's what I found:\n\n{searchResults.Results[0].Snippet}";

        var citations = searchResults.Results.Select((r, idx) => new Citation
        {
            DocumentId = r.DocumentId,
            Type = r.Type,
            Title = r.Title,
            Excerpt = r.Snippet,
            RelevanceRank = idx + 1,
            SourceUri = r.SourceUri
        }).ToList();

        _logger.LogInformation("Q&A query '{Question}' generated answer with {CitationCount} citations", request.Question, citations.Count);

        return new QaResponse
        {
            Answer = answer,
            Citations = citations,
            Confidence = searchResults.Results.FirstOrDefault()?.Score ?? 0.0,
            RelatedQuestions = GenerateRelatedQuestions(request.Question)
        };
    }

    public async Task RecordFeedbackAsync(SearchFeedback feedback, CancellationToken ct = default)
    {
        // Store feedback for later analysis (could use separate feedback index or append to document metadata)
        _logger.LogInformation("Feedback recorded: {Type} for query '{Query}' on doc {DocId}", feedback.Type, feedback.Query, feedback.DocumentId);
        await Task.CompletedTask;
    }

    private List<string> GenerateRelatedQuestions(string question)
    {
        // Simple implementation; in production, use LLM or pre-generated suggestions
        return new List<string>
        {
            $"What is the process for {question}?",
            $"How do I handle {question}?",
            $"Where can I find documentation on {question}?"
        };
    }
}

