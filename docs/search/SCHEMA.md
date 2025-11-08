# Search Index Schema and Authoring Best Practices

## Azure Cognitive Search Index Schema

### Fields
- **id** (String, Key): Unique chunk identifier (e.g., "doc-123-chunk-0")
- **documentId** (String, Filterable, Facetable): Original entity ID (e.g., permit GUID)
- **type** (String, Filterable, Facetable, Searchable): Document type enum
- **title** (String, Searchable, Sortable): Document title
- **content** (String, Searchable): Full text content of chunk
- **tenantId** (String, Filterable, Facetable): Tenant identifier
- **siteId** (String, Filterable, Facetable): Site identifier (optional)
- **assetId** (String, Filterable, Facetable): Asset identifier (optional)
- **tags** (Collection[String], Filterable, Facetable): Tags for categorization
- **roles** (Collection[String], Filterable): Required roles to view document
- **indexedAt** (DateTimeOffset, Sortable, Filterable): When document was indexed
- **documentDate** (DateTimeOffset, Sortable, Filterable): Original document date
- **sourceUri** (String): URI to original document
- **chunkOffset** (Int32): Character offset of chunk in original document
- **chunkLength** (Int32): Length of chunk in characters
- **embedding** (Collection[Single]): Vector embedding for semantic search (1536 dimensions)
- **metadata_*** (Dynamic): Custom metadata fields

### Semantic Configuration
- **Configuration Name**: "default"
- **Title Field**: "title"
- **Content Fields**: ["content"]

### Suggesters
- **Name**: "sg"
- **Fields**: ["title", "content"]

## Authoring Best Practices

### Document Structure
1. **Clear Titles**: Use descriptive, keyword-rich titles
   - ✅ Good: "Gas Lift Optimization Procedure for ESP Wells"
   - ❌ Bad: "Procedure #123"

2. **Metadata Tagging**: Tag documents with relevant categories
   - Examples: ["production", "safety", "maintenance", "optimization"]
   - Use consistent tag vocabulary

3. **Role Assignment**: Assign appropriate roles for access control
   - Examples: ["ControlRoomOperator", "ProductionEngineer", "HSELead"]
   - Don't over-restrict; use least-privilege principle

4. **Date Information**: Include documentDate for temporal filtering
   - Use original document date, not indexing date

5. **Source URI**: Include link to original document for drill-down
   - Format: `/api/v1/permits/{id}` or external URL

### Content Guidelines
1. **Plain Text**: Prefer plain text content; extract from PDFs/Word docs
2. **Section Headers**: Use clear section headers (will be chunked at boundaries)
3. **Keywords**: Include technical terms and acronyms (acronym expansion handled separately)
4. **No PII**: Redact personally identifiable information before indexing
5. **Consistent Terminology**: Use consistent terminology across documents (enables better synonym mapping)

### Chunking Considerations
1. **Self-Contained Chunks**: Each chunk should be understandable on its own
2. **Context Preservation**: Important context (e.g., "this procedure applies to ESP wells") should appear in multiple chunks
3. **Avoid Mid-Sentence Breaks**: Chunking respects sentence boundaries when possible

### Security Best Practices
1. **Tenant Isolation**: Always specify tenantId
2. **Role Minimums**: Assign minimum required roles; don't make everything "Admin" only
3. **Site/Asset Scoping**: Use siteId/assetId for asset-specific documents
4. **Audit Trail**: Document who indexed what and when

## Acronym Mapping

Common Oil & Gas acronyms to expand in search:
- **ESP**: Electric Submersible Pump
- **GL**: Gas Lift
- **PTW**: Permit-to-Work
- **LOTO**: Lock-Out Tag-Out
- **HSE**: Health, Safety, Environment
- **RBI**: Risk-Based Inspection
- **PVT**: Pressure-Volume-Temperature
- **BS&W**: Basic Sediment and Water
- **GOR**: Gas-Oil Ratio
- **WC**: Water Cut
- **CUST**: Custody Transfer
- **ADX**: Azure Data Explorer

## Synonym Mapping (Azure Cognitive Search)

Configure synonyms in Azure Portal or via API:
```
ESP, Electric Submersible Pump, submersible pump
gas lift, GL, gas injection
permit, PTW, permit-to-work
lockout, LOTO, lock-out tag-out
```

## Quality Checklist

Before indexing a document:
- [ ] Title is descriptive and keyword-rich
- [ ] Content is plain text (no formatting artifacts)
- [ ] Metadata tags are assigned
- [ ] Required roles are specified
- [ ] Tenant/site/asset IDs are correct
- [ ] Document date is accurate
- [ ] Source URI points to original document
- [ ] No PII or sensitive data in content
- [ ] Acronyms are expanded in content or metadata
- [ ] Chunking will produce meaningful segments

