using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using IOC.Search.Models;
using IOC.Search.Indexing;

namespace IOC.Search.Indexing;

/// <summary>
/// Azure Cognitive Search implementation of document indexing
/// </summary>
public sealed class AzureSearchIndexer : IDocumentIndexer
{
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly DocumentChunker _chunker;
    private readonly IEmbeddingGenerator? _embeddingGenerator;
    private readonly ILogger<AzureSearchIndexer> _logger;
    private const string IndexName = "deltagrid-documents";

    public AzureSearchIndexer(
        string searchEndpoint,
        string searchApiKey,
        DocumentChunker chunker,
        IEmbeddingGenerator? embeddingGenerator,
        ILogger<AzureSearchIndexer> logger)
    {
        var credential = new AzureKeyCredential(searchApiKey);
        _indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);
        _searchClient = new SearchClient(new Uri(searchEndpoint), IndexName, credential);
        _chunker = chunker;
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
    }

    public async Task IndexAsync(SearchableDocument doc, CancellationToken ct = default)
    {
        await EnsureIndexExistsAsync(ct);

        var chunks = _chunker.Chunk(doc);
        var indexActions = new List<IndexDocumentsAction<SearchDocument>>();

        foreach (var chunk in chunks)
        {
            var searchDoc = new SearchDocument
            {
                ["id"] = chunk.Id,
                ["documentId"] = doc.DocumentId,
                ["type"] = doc.Type.ToString(),
                ["title"] = doc.Title,
                ["content"] = chunk.Content,
                ["tenantId"] = doc.TenantId,
                ["siteId"] = doc.SiteId ?? string.Empty,
                ["assetId"] = doc.AssetId ?? string.Empty,
                ["tags"] = doc.Tags.ToArray(),
                ["roles"] = doc.Roles.ToArray(),
                ["indexedAt"] = doc.IndexedAt,
                ["documentDate"] = doc.DocumentDate,
                ["sourceUri"] = doc.SourceUri ?? string.Empty,
                ["chunkOffset"] = chunk.Offset,
                ["chunkLength"] = chunk.Length
            };

            // Add metadata fields
            foreach (var (key, value) in doc.Metadata)
            {
                searchDoc[$"metadata_{key}"] = value;
            }

            // Generate embedding if available
            if (_embeddingGenerator != null)
            {
                var embedding = await _embeddingGenerator.GenerateAsync(chunk.Content, ct);
                searchDoc["embedding"] = embedding;
            }

            indexActions.Add(IndexDocumentsAction.Upload(searchDoc));
        }

        if (indexActions.Any())
        {
            var batch = IndexDocumentsBatch.Create(indexActions.ToArray());
            var response = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: ct);
            _logger.LogInformation("Indexed document {DocumentId} as {ChunkCount} chunks", doc.DocumentId, chunks.Count);
        }
    }

    public async Task IndexBatchAsync(IEnumerable<SearchableDocument> docs, CancellationToken ct = default)
    {
        await EnsureIndexExistsAsync(ct);

        foreach (var doc in docs)
        {
            await IndexAsync(doc, ct);
        }
    }

    public async Task DeleteAsync(string documentId, CancellationToken ct = default)
    {
        // Delete all chunks for this document
        var filter = $"documentId eq '{documentId}'";
        var searchOptions = new SearchOptions { Filter = filter, Size = 1000 };
        var results = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions, ct).ConfigureAwait(false);

        var deleteActions = new List<IndexDocumentsAction<SearchDocument>>();
        await foreach (var result in results.Value.GetResultsAsync())
        {
            deleteActions.Add(IndexDocumentsAction.Delete(new SearchDocument { ["id"] = result.Document["id"].ToString() }));
        }

        if (deleteActions.Any())
        {
            var batch = IndexDocumentsBatch.Create(deleteActions.ToArray());
            await _searchClient.IndexDocumentsAsync(batch, cancellationToken: ct);
            _logger.LogInformation("Deleted document {DocumentId} and {ChunkCount} chunks", documentId, deleteActions.Count);
        }
    }

    public async Task UpdateAsync(SearchableDocument doc, CancellationToken ct = default)
    {
        await DeleteAsync(doc.DocumentId, ct);
        await IndexAsync(doc, ct);
    }

    public async Task ReindexTypeAsync(DocumentType type, CancellationToken ct = default)
    {
        // This would typically trigger a full re-index from source systems
        _logger.LogWarning("ReindexTypeAsync not fully implemented; requires source system integration");
        await Task.CompletedTask;
    }

    private async Task EnsureIndexExistsAsync(CancellationToken ct)
    {
        try
        {
            await _indexClient.GetIndexAsync(IndexName, ct);
            return;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Index doesn't exist, create it
        }

        var definition = new SearchIndex(IndexName)
        {
            Fields =
            {
                new SearchField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchField("documentId", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchField("type", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true, IsSearchable = true },
                new SearchField("title", SearchFieldDataType.String) { IsSearchable = true, IsSortable = true },
                new SearchField("content", SearchFieldDataType.String) { IsSearchable = true },
                new SearchField("tenantId", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchField("siteId", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchField("assetId", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchField("tags", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true, IsFacetable = true },
                new SearchField("roles", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true },
                new SearchField("indexedAt", SearchFieldDataType.DateTimeOffset) { IsSortable = true, IsFilterable = true },
                new SearchField("documentDate", SearchFieldDataType.DateTimeOffset) { IsSortable = true, IsFilterable = true },
                new SearchField("sourceUri", SearchFieldDataType.String),
                new SearchField("chunkOffset", SearchFieldDataType.Int32),
                new SearchField("chunkLength", SearchFieldDataType.Int32),
                new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Double)) { IsFilterable = false }
            },
            Suggesters = { new SearchSuggester("sg", new[] { "title", "content" }) }
            // SemanticSettings removed - requires premium tier
        };

        await _indexClient.CreateIndexAsync(definition, ct);
        _logger.LogInformation("Created search index {IndexName}", IndexName);
    }
}

/// <summary>
/// Generates vector embeddings for semantic search
/// </summary>
public interface IEmbeddingGenerator
{
    Task<float[]> GenerateAsync(string text, CancellationToken ct = default);
}

/// <summary>
/// Azure OpenAI embedding generator
/// </summary>
public sealed class AzureOpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly Azure.AI.OpenAI.OpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAIEmbeddingGenerator> _logger;

    public AzureOpenAIEmbeddingGenerator(
        string endpoint,
        string apiKey,
        string deploymentName,
        ILogger<AzureOpenAIEmbeddingGenerator> logger)
    {
        _client = new Azure.AI.OpenAI.OpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
        _deploymentName = deploymentName;
        _logger = logger;
    }

    public async Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var options = new Azure.AI.OpenAI.EmbeddingsOptions(_deploymentName, new[] { text });
            var response = await _client.GetEmbeddingsAsync(options, ct);
            return response.Value.Data[0].Embedding.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding");
            // Return zero vector as fallback
            return new float[1536]; // Standard embedding size
        }
    }
}

