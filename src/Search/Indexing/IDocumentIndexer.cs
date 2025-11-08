using IOC.Search.Models;

namespace IOC.Search.Indexing;

/// <summary>
/// Indexes documents into Azure Cognitive Search with chunking and embeddings
/// </summary>
public interface IDocumentIndexer
{
    /// <summary>
    /// Index a document with automatic chunking and embedding generation
    /// </summary>
    Task IndexAsync(SearchableDocument doc, CancellationToken ct = default);

    /// <summary>
    /// Index multiple documents in batch
    /// </summary>
    Task IndexBatchAsync(IEnumerable<SearchableDocument> docs, CancellationToken ct = default);

    /// <summary>
    /// Delete a document from the index
    /// </summary>
    Task DeleteAsync(string documentId, CancellationToken ct = default);

    /// <summary>
    /// Update an existing indexed document
    /// </summary>
    Task UpdateAsync(SearchableDocument doc, CancellationToken ct = default);

    /// <summary>
    /// Re-index all documents of a specific type (for schema changes)
    /// </summary>
    Task ReindexTypeAsync(DocumentType type, CancellationToken ct = default);
}

