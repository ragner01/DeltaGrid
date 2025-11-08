using Asp.Versioning;
using IOC.Search.Indexing;
using IOC.Search.Models;
using IOC.Search.Querying;
using IOC.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization(opt =>



{
    opt.AddPolicy("TenantScoped", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(ctx => ctx.User.HasClaim(c => c.Type == ClaimsSchema.TenantId));
    });

});

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer();

// Search service configuration
var searchEndpoint = builder.Configuration["Search:Endpoint"] ?? "https://deltagrid-search.search.windows.net";
var searchApiKey = builder.Configuration["Search:ApiKey"] ?? "placeholder-key";

// Optional: Azure OpenAI for embeddings and Q&A
var openAiEndpoint = builder.Configuration["OpenAI:Endpoint"];
var openAiKey = builder.Configuration["OpenAI:ApiKey"];
var openAiDeployment = builder.Configuration["OpenAI:EmbeddingDeployment"] ?? "text-embedding-ada-002";

if (!string.IsNullOrEmpty(openAiEndpoint) && !string.IsNullOrEmpty(openAiKey))
{
    builder.Services.AddSingleton<IEmbeddingGenerator>(sp =>
        new AzureOpenAIEmbeddingGenerator(openAiEndpoint!, openAiKey!, openAiDeployment,
            sp.GetRequiredService<ILogger<AzureOpenAIEmbeddingGenerator>>()));
}

builder.Services.AddSingleton<DocumentChunker>();

builder.Services.AddSingleton<IDocumentIndexer>(sp =>
{
    var chunker = sp.GetRequiredService<DocumentChunker>();
    var embedding = sp.GetService<IEmbeddingGenerator>();
    var logger = sp.GetRequiredService<ILogger<AzureSearchIndexer>>();
    return new AzureSearchIndexer(searchEndpoint, searchApiKey, chunker, embedding, logger);
});

builder.Services.AddSingleton<ISearchService>(sp =>
{
    var embedding = sp.GetService<IEmbeddingGenerator>();
    var logger = sp.GetRequiredService<ILogger<AzureSearchService>>();
    return new AzureSearchService(searchEndpoint, searchApiKey, embedding, logger);
});

// Indexing pipelines
builder.Services.AddSingleton<IIndexingPipeline, SopIndexingPipeline>();
builder.Services.AddSingleton<IIndexingPipeline, PermitIndexingPipeline>();
builder.Services.AddSingleton<IIndexingPipeline, LabResultIndexingPipeline>();
builder.Services.AddSingleton<IIndexingPipeline, IncidentIndexingPipeline>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithMetadata(new ApiVersion(1, 0))
    .RequireAuthorization("TenantScoped");

// Search endpoint
v1.MapPost("/search", async (SearchQuery query, ISearchService searchService, HttpContext http, CancellationToken ct) =>
{
    // Extract tenant and roles from claims for security trimming
    var tenantId = http.User.FindFirst(ClaimsSchema.TenantId)?.Value;
    var roles = http.User.Claims
        .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role")
        .Select(c => c.Value)
        .ToList();

    var secureQuery = query with
    {
        TenantId = tenantId ?? query.TenantId,
        RequiredRoles = roles.Any() ? roles : query.RequiredRoles
    };

    var response = await searchService.SearchAsync(secureQuery, ct);
    return Results.Ok(response);
});

// Q&A endpoint ("Ask Ops")
v1.MapPost("/search/qa", async (QaRequest request, ISearchService searchService, HttpContext http, CancellationToken ct) =>
{
    var tenantId = http.User.FindFirst(ClaimsSchema.TenantId)?.Value;
    var roles = http.User.Claims
        .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role")
        .Select(c => c.Value)
        .ToList();

    var secureRequest = request with
    {
        TenantId = tenantId ?? request.TenantId,
        RequiredRoles = roles.Any() ? roles : request.RequiredRoles
    };

    var response = await searchService.AskOpsAsync(secureRequest, ct);
    return Results.Ok(response);
});

// Feedback endpoint
v1.MapPost("/search/feedback", async (SearchFeedback feedback, ISearchService searchService, HttpContext http, CancellationToken ct) =>
{
    var userId = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var feedbackWithUser = feedback with { UserId = userId ?? feedback.UserId };
    await searchService.RecordFeedbackAsync(feedbackWithUser, ct);
    return Results.NoContent();
});

// Index document (for manual indexing)
v1.MapPost("/search/index", async (SearchableDocument doc, IDocumentIndexer indexer, HttpContext http, CancellationToken ct) =>
{
    var tenantId = http.User.FindFirst(ClaimsSchema.TenantId)?.Value;
    if (string.IsNullOrEmpty(tenantId) || doc.TenantId != tenantId)
    {
        return Results.Forbid();
    }

    await indexer.IndexAsync(doc, ct);
    return Results.NoContent();
});

// Trigger reindex for a document type
v1.MapPost("/search/reindex/{type}", async (DocumentType type, IDocumentIndexer indexer, CancellationToken ct) =>
{
    await indexer.ReindexTypeAsync(type, ct);
    return Results.Accepted();
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "search" }));

app.Run();

public partial class Program { }

