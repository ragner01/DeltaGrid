# Enterprise Search and Knowledge Architecture

## Overview
Phase 22 delivers secure semantic search across SOPs, incidents, permits, lab results, and other operational documents. The system uses Azure Cognitive Search with vector embeddings for semantic search and provides a Q&A endpoint ("Ask Ops") powered by LLM integration.

## Architecture

### Components
1. **Indexing Service**: Chunks documents, generates embeddings, indexes into Azure Cognitive Search
2. **Search Service**: Handles queries with security trimming by tenant/role
3. **Q&A Service**: Semantic search + LLM for natural language answers with citations
4. **Feedback Loop**: Captures user feedback for relevancy tuning

### Security Model
- **Tenant Isolation**: All queries automatically filtered by tenant ID from JWT claims
- **Role-Based Trimming**: Results filtered by user roles (e.g., only show permits user can view)
- **Path-Scoped Access**: Integration with Digital Twin path-scoped authorization for asset-specific documents
- **Redaction Rules**: PII and sensitive data redacted before indexing (future enhancement)

### Document Types
- **SOPs**: Standard Operating Procedures
- **Incidents**: Incident reports and investigations
- **Permits**: Permit-to-Work documents
- **Lab Results**: Laboratory analysis results and certificates
- **Work Orders**: Work order descriptions and histories
- **Integrity Reports**: Inspection and integrity findings
- **Allocation Reports**: Production allocation summaries
- **Custody Tickets**: Custody transfer tickets

### Chunking Strategy
- Default chunk size: 1000 characters
- Overlap: 200 characters for context continuity
- Sentence boundary awareness for clean breaks
- Metadata preservation across chunks

### Indexing Pipeline
1. Document ingestion (manual or automated from source systems)
2. Chunking with overlap
3. Embedding generation (Azure OpenAI text-embedding-ada-002)
4. Index upload to Azure Cognitive Search
5. Metadata tagging (tenant, site, asset, roles, tags)

### Search Flow
1. User submits query
2. Security context extracted from JWT (tenant, roles)
3. Query filtered by tenant + role permissions
4. Semantic/vector search if embeddings available, else keyword search
5. Results trimmed and ranked
6. Highlights and snippets generated
7. Response returned with facets

### Q&A Flow
1. User asks question
2. Semantic search finds top 3-5 relevant document chunks
3. Context assembled from chunks
4. LLM generates answer with citations
5. Related questions suggested
6. Confidence score calculated

## Configuration

```json
{
  "Search": {
    "Endpoint": "https://deltagrid-search.search.windows.net",
    "ApiKey": "<key>",
    "IndexName": "deltagrid-documents"
  },
  "OpenAI": {
    "Endpoint": "https://deltagrid-openai.openai.azure.com",
    "ApiKey": "<key>",
    "EmbeddingDeployment": "text-embedding-ada-002",
    "ChatDeployment": "gpt-4" // For Q&A
  }
}
```

## API Endpoints

### POST /api/v1/search
Search documents with filters.

Request:
```json
{
  "query": "gas lift optimization",
  "typeFilter": "SOP",
  "tags": ["production", "optimization"],
  "skip": 0,
  "take": 10,
  "semantic": true
}
```

Response:
```json
{
  "results": [
    {
      "id": "chunk-1",
      "documentId": "sop-123",
      "type": "SOP",
      "title": "Gas Lift Optimization Guide",
      "snippet": "...",
      "score": 0.95,
      "highlights": ["gas lift", "optimization"]
    }
  ],
  "totalCount": 42,
  "facets": {
    "type": [{"value": "SOP", "count": 25}],
    "tags": [{"value": "production", "count": 15}]
  },
  "elapsedMs": "00:00:00.123"
}
```

### POST /api/v1/search/qa
Ask a question and get an answer with citations.

Request:
```json
{
  "question": "How do I optimize gas lift rates?",
  "scope": "SOP",
  "maxContextDocs": 5
}
```

Response:
```json
{
  "answer": "Based on the available documents, gas lift optimization involves...",
  "citations": [
    {
      "documentId": "sop-123",
      "type": "SOP",
      "title": "Gas Lift Optimization Guide",
      "excerpt": "...",
      "relevanceRank": 1
    }
  ],
  "confidence": 0.92,
  "relatedQuestions": [
    "What is the process for gas lift optimization?",
    "How do I handle gas lift rate changes?"
  ]
}
```

### POST /api/v1/search/feedback
Record search feedback for relevancy tuning.

Request:
```json
{
  "query": "gas lift optimization",
  "documentId": "sop-123",
  "type": "Relevant",
  "comment": "Very helpful"
}
```

## Indexing

### Manual Indexing
```bash
POST /api/v1/search/index
{
  "id": "doc-1",
  "documentId": "permit-456",
  "type": "Permit",
  "title": "Hot Work Permit #456",
  "content": "Full permit text...",
  "tenantId": "tenant-1",
  "siteId": "site-1",
  "tags": ["hot-work", "safety"],
  "roles": ["ControlRoomOperator", "HSELead"],
  "documentDate": "2025-10-30T10:00:00Z"
}
```

### Automated Pipelines
- **SOP Pipeline**: Indexes SOPs from CMS/file system
- **Permit Pipeline**: Indexes permits from PTW repository
- **Lab Pipeline**: Indexes lab results from lab repository
- **Incident Pipeline**: Indexes incidents from events repository

## Relevancy Tuning

1. **Synonym Mapping**: Configure synonyms in Azure Cognitive Search (e.g., "ESP" = "Electric Submersible Pump")
2. **Acronym Mapping**: Pre-define acronyms in document metadata
3. **Feedback Analysis**: Aggregate feedback to identify low-relevance queries
4. **Score Boosting**: Boost certain fields (title, tags) in search scoring

## Testing

### Relevance Tests
- Curated query set with expected top-3 results
- Target: ≥80% of queries have relevant doc in top-3
- Run on index snapshots to track regressions

### Security Tests
- Tenant isolation verified (tenant-1 cannot see tenant-2 docs)
- Role trimming verified (user without "HSELead" cannot see HSELead-only docs)
- Leakage tests: query with invalid tenant/role returns empty results

### Performance Tests
- Query latency P95 < 500ms
- Indexing throughput: 100 docs/sec
- Q&A latency P95 < 3s

## Observability

- Query success rate
- Click-through rate (CTR) on search results
- Abandonment rate (queries with no clicks)
- Q&A confidence score distribution
- Indexing lag (time from source update to searchable)

## Future Enhancements

- **LLM Integration**: Full Azure OpenAI Chat API for Q&A (currently placeholder)
- **Redaction Rules**: Automated PII/sensitive data redaction before indexing
- **Multi-language**: Support for multiple languages with translation
- **Auto-learning**: Feedback-driven query expansion and synonym discovery
- **External Search**: Integration with external knowledge bases (future)

