using System.Text;
using IOC.Search.Models;

namespace IOC.Search.Indexing;

/// <summary>
/// Chunks documents into searchable segments with metadata preservation
/// </summary>
public sealed class DocumentChunker
{
    private const int DefaultChunkSize = 1000; // characters
    private const int OverlapSize = 200; // characters for context overlap

    /// <summary>
    /// Chunk a document into smaller segments while preserving metadata
    /// </summary>
    public List<ChunkedSegment> Chunk(SearchableDocument doc, int maxChunkSize = DefaultChunkSize)
    {
        if (doc.Content.Length <= maxChunkSize)
        {
            return new List<ChunkedSegment>
            {
                new(doc.Id, doc.Content, 0, doc.Content.Length, doc)
            };
        }

        var chunks = new List<ChunkedSegment>();
        var content = doc.Content;
        var offset = 0;
        var chunkIndex = 0;

        while (offset < content.Length)
        {
            var end = Math.Min(offset + maxChunkSize, content.Length);

            // Try to break at sentence boundaries
            if (end < content.Length)
            {
                var lastPeriod = content.LastIndexOf('.', end - 1, maxChunkSize);
                var lastNewline = content.LastIndexOf('\n', end - 1, maxChunkSize);

                var breakPoint = Math.Max(lastPeriod, lastNewline);
                if (breakPoint > offset + maxChunkSize / 2) // Don't make chunks too small
                {
                    end = breakPoint + 1;
                }
            }

            var chunkContent = content.Substring(offset, end - offset);
            chunks.Add(new ChunkedSegment(
                $"{doc.Id}-chunk-{chunkIndex}",
                chunkContent,
                offset,
                end - offset,
                doc
            ));

            // Overlap for context continuity
            offset = Math.Max(offset + maxChunkSize - OverlapSize, end);
            chunkIndex++;
        }

        return chunks;
    }
}

/// <summary>
/// A chunked segment with position and parent document metadata
/// </summary>
public sealed record ChunkedSegment(
    string Id,
    string Content,
    int Offset,
    int Length,
    SearchableDocument Parent
);

