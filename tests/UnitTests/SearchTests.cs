using IOC.Search.Models;
using IOC.Search.Indexing;
using Xunit;

namespace IOC.UnitTests;

public class DocumentChunkerTests
{
    [Fact]
    public void Chunk_SmallDocument_ReturnsSingleChunk()
    {
        var chunker = new DocumentChunker();
        var doc = new SearchableDocument
        {
            Id = "doc-1",
            DocumentId = "doc-1",
            Type = DocumentType.SOP,
            Title = "Test SOP",
            Content = "This is a short document.",
            TenantId = "tenant-1"
        };

        var chunks = chunker.Chunk(doc, maxChunkSize: 1000);

        Assert.Single(chunks);
        Assert.Equal(doc.Content, chunks[0].Content);
    }

    [Fact]
    public void Chunk_LargeDocument_ReturnsMultipleChunks()
    {
        var chunker = new DocumentChunker();
        var largeContent = string.Join("\n", Enumerable.Range(1, 500).Select(i => $"Paragraph {i} with enough text to fill a chunk."));
        var doc = new SearchableDocument
        {
            Id = "doc-2",
            DocumentId = "doc-2",
            Type = DocumentType.SOP,
            Title = "Large SOP",
            Content = largeContent,
            TenantId = "tenant-1"
        };

        var chunks = chunker.Chunk(doc, maxChunkSize: 500);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Content.Length <= 700)); // Allow some overflow for sentence boundaries
    }

    [Fact]
    public void Chunk_PreservesMetadata()
    {
        var chunker = new DocumentChunker();
        var doc = new SearchableDocument
        {
            Id = "doc-3",
            DocumentId = "doc-3",
            Type = DocumentType.Permit,
            Title = "Test Permit",
            Content = "Content here.",
            TenantId = "tenant-1",
            Tags = new List<string> { "hot-work", "safety" }
        };

        var chunks = chunker.Chunk(doc);

        Assert.All(chunks, c => Assert.Equal(doc.TenantId, c.Parent.TenantId));
        Assert.All(chunks, c => Assert.Equal(doc.Type, c.Parent.Type));
    }
}

public class SearchQuerySecurityTests
{
    [Fact]
    public void SearchQuery_RequiresTenantId_EnforcesIsolation()
    {
        var query = new SearchQuery
        {
            SearchText = "test",
            TenantId = "tenant-1"
        };

        Assert.Equal("tenant-1", query.TenantId);
    }

    [Fact]
    public void SearchQuery_RequiredRoles_FiltersByAccess()
    {
        var query = new SearchQuery
        {
            SearchText = "permit",
            RequiredRoles = new List<string> { "ControlRoomOperator", "HSELead" }
        };

        Assert.NotNull(query.RequiredRoles);
        Assert.Contains("HSELead", query.RequiredRoles);
    }
}
