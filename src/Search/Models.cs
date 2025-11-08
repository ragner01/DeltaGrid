namespace IOC.Search.Models;

/// <summary>
/// Document types that can be indexed and searched
/// </summary>
public enum DocumentType
{
    SOP,
    Incident,
    Permit,
    LabResult,
    WorkOrder,
    IntegrityReport,
    AllocationReport,
    CustodyTicket
}

/// <summary>
/// Searchable document with metadata and content chunks
/// </summary>
public sealed class SearchableDocument
{
    public required string Id { get; init; }
    public required string DocumentId { get; init; } // Original entity ID (e.g., permit GUID)
    public required DocumentType Type { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required string TenantId { get; init; }
    public string? SiteId { get; init; }
    public string? AssetId { get; init; }
    public List<string> Tags { get; init; } = new();
    public List<string> Roles { get; init; } = new(); // Roles required to view
    public DateTimeOffset IndexedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DocumentDate { get; init; }
    public string? SourceUri { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public float[]? Embedding { get; init; } // Vector embedding for semantic search
}

/// <summary>
/// Search query with filters and pagination
/// </summary>
public sealed record SearchQuery
{
    public required string SearchText { get; init; }
    public DocumentType? TypeFilter { get; init; }
    public string? TenantId { get; init; }
    public string? SiteId { get; init; }
    public string? AssetId { get; init; }
    public List<string>? Tags { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public bool Semantic { get; init; } = true; // Use vector search if available
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 10;
    public List<string>? RequiredRoles { get; init; } // Current user roles for security trimming
    public string? VectorQuery { get; init; } // Vector query text for semantic search
}

/// <summary>
/// Search result with relevance score and highlights
/// </summary>
public sealed class SearchResult
{
    public required string Id { get; init; }
    public required string DocumentId { get; init; }
    public required DocumentType Type { get; init; }
    public required string Title { get; init; }
    public required string Snippet { get; init; }
    public double Score { get; init; }
    public DateTimeOffset? DocumentDate { get; init; }
    public string? SourceUri { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public List<string> Highlights { get; init; } = new();
}

/// <summary>
/// Q&A request for "Ask Ops" functionality
/// </summary>
public sealed record QaRequest
{
    public required string Question { get; init; }
    public DocumentType? Scope { get; init; } // Limit to specific doc types
    public string? TenantId { get; init; }
    public int MaxContextDocs { get; init; } = 5;
    public List<string>? RequiredRoles { get; init; }
}

/// <summary>
/// Q&A response with answer and citations
/// </summary>
public sealed class QaResponse
{
    public required string Answer { get; init; }
    public List<Citation> Citations { get; init; } = new();
    public double Confidence { get; init; }
    public List<string> RelatedQuestions { get; init; } = new();
}

/// <summary>
/// Citation reference to source document
/// </summary>
public sealed class Citation
{
    public required string DocumentId { get; init; }
    public required DocumentType Type { get; init; }
    public required string Title { get; init; }
    public required string Excerpt { get; init; }
    public int RelevanceRank { get; init; }
    public string? SourceUri { get; init; }
}

/// <summary>
/// Search feedback for relevancy tuning
/// </summary>
public sealed record SearchFeedback
{
    public required string Query { get; init; }
    public required string DocumentId { get; init; }
    public required FeedbackType Type { get; init; }
    public int Rating { get; init; } // 1-5 rating
    public string? Comment { get; init; }
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? UserId { get; init; }
}

public enum FeedbackType
{
    Relevant,
    NotRelevant,
    Clicked,
    Abandoned
}

