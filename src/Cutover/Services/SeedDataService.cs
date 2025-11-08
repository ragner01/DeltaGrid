using IOC.Cutover.Models;

namespace IOC.Cutover.Services;

/// <summary>
/// Seed data service implementation
/// </summary>
public sealed class SeedDataService : ISeedDataService
{
    private readonly ISeedDataRepository _repository;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        ISeedDataRepository repository,
        ILogger<SeedDataService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SeedResult> SeedAllAsync(string createdBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding all demo data");

        var results = new List<SeedResult>();

        foreach (var type in Enum.GetValues<SeedDataType>())
        {
            var result = await SeedAsync(type, createdBy, ct);
            results.Add(result);
        }

        var totalCreated = results.Sum(r => r.RecordsCreated);
        var totalFailed = results.Sum(r => r.RecordsFailed);
        var allErrors = results.SelectMany(r => r.Errors).ToList();

        _logger.LogInformation("Seeding completed: {Created} created, {Failed} failed", totalCreated, totalFailed);

        return new SeedResult
        {
            Type = SeedDataType.Tenant,  // Placeholder for aggregate
            RecordsCreated = totalCreated,
            RecordsFailed = totalFailed,
            Errors = allErrors,
            CompletedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<SeedResult> SeedAsync(SeedDataType type, string createdBy, CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding {Type} data", type);

        var errors = new List<string>();
        int created = 0;
        int failed = 0;

        try
        {
            var seedData = GenerateSeedData(type, createdBy);

            foreach (var data in seedData)
            {
                try
                {
                    await _repository.SaveSeedDataAsync(data, ct);
                    created++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"Failed to seed {data.Name}: {ex.Message}");
                    _logger.LogError(ex, "Failed to seed {Name}", data.Name);
                }
            }

            _logger.LogInformation("Seeded {Type}: {Created} created, {Failed} failed", type, created, failed);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to seed {type}: {ex.Message}");
            _logger.LogError(ex, "Failed to seed {Type}", type);
        }

        return new SeedResult
        {
            Type = type,
            RecordsCreated = created,
            RecordsFailed = failed,
            Errors = errors,
            CompletedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<ValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Validating seed data");

        var issues = new List<ValidationIssue>();
        var recordCounts = new Dictionary<SeedDataType, int>();

        foreach (var type in Enum.GetValues<SeedDataType>())
        {
            var count = await _repository.GetSeedDataCountAsync(type, ct);
            recordCounts[type] = count;

            // Validate minimum counts
            var minCount = GetMinimumCount(type);
            if (count < minCount)
            {
                issues.Add(new ValidationIssue
                {
                    Type = type,
                    Issue = $"Expected at least {minCount} records, found {count}",
                    Severity = "Error"
                });
            }
        }

        var isValid = !issues.Any(i => i.Severity == "Error");

        _logger.LogInformation("Validation completed: Valid = {IsValid}, Issues = {IssueCount}", isValid, issues.Count);

        return new ValidationResult
        {
            IsValid = isValid,
            Issues = issues,
            RecordCounts = recordCounts
        };
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        _logger.LogWarning("Clearing all seed data");

        foreach (var type in Enum.GetValues<SeedDataType>())
        {
            await _repository.ClearSeedDataAsync(type, ct);
        }

        _logger.LogInformation("Seed data cleared");
    }

    private List<SeedData> GenerateSeedData(SeedDataType type, string createdBy)
    {
        return type switch
        {
            SeedDataType.Tenant => GenerateTenantData(createdBy),
            SeedDataType.Asset => GenerateAssetData(createdBy),
            SeedDataType.Well => GenerateWellData(createdBy),
            SeedDataType.Meter => GenerateMeterData(createdBy),
            SeedDataType.LabReference => GenerateLabReferenceData(createdBy),
            SeedDataType.User => GenerateUserData(createdBy),
            SeedDataType.Role => GenerateRoleData(createdBy),
            _ => new List<SeedData>()
        };
    }

    private List<SeedData> GenerateTenantData(string createdBy)
    {
        return new List<SeedData>
        {
            new SeedData
            {
                Id = "tenant-001",
                Type = SeedDataType.Tenant,
                Name = "Nigerian JV Partner A",
                Data = new { TenantId = "tenant-001", Name = "Nigerian JV Partner A", Region = "Lagos" },
                CreatedBy = createdBy
            },
            new SeedData
            {
                Id = "tenant-002",
                Type = SeedDataType.Tenant,
                Name = "Nigerian JV Partner B",
                Data = new { TenantId = "tenant-002", Name = "Nigerian JV Partner B", Region = "Port Harcourt" },
                CreatedBy = createdBy
            }
        };
    }

    private List<SeedData> GenerateAssetData(string createdBy)
    {
        return new List<SeedData>
        {
            new SeedData
            {
                Id = "asset-001",
                Type = SeedDataType.Asset,
                Name = "Delta Field",
                Data = new { AssetId = "asset-001", Name = "Delta Field", TenantId = "tenant-001", Region = "Niger Delta" },
                CreatedBy = createdBy
            },
            new SeedData
            {
                Id = "asset-002",
                Type = SeedDataType.Asset,
                Name = "Omega Platform",
                Data = new { AssetId = "asset-002", Name = "Omega Platform", TenantId = "tenant-002", Region = "Offshore" },
                CreatedBy = createdBy
            }
        };
    }

    private List<SeedData> GenerateWellData(string createdBy)
    {
        return new List<SeedData>
        {
            new SeedData
            {
                Id = "well-001",
                Type = SeedDataType.Well,
                Name = "WELL-001",
                Data = new { WellId = "well-001", Name = "WELL-001", AssetId = "asset-001", Type = "Production" },
                CreatedBy = createdBy
            },
            new SeedData
            {
                Id = "well-002",
                Type = SeedDataType.Well,
                Name = "WELL-002",
                Data = new { WellId = "well-002", Name = "WELL-002", AssetId = "asset-001", Type = "Production" },
                CreatedBy = createdBy
            },
            new SeedData
            {
                Id = "well-003",
                Type = SeedDataType.Well,
                Name = "WELL-003",
                Data = new { WellId = "well-003", Name = "WELL-003", AssetId = "asset-002", Type = "Production" },
                CreatedBy = createdBy
            }
        };
    }

    private List<SeedData> GenerateMeterData(string createdBy)
    {
        return new List<SeedData>
        {
            new SeedData
            {
                Id = "meter-001",
                Type = SeedDataType.Meter,
                Name = "Fiscal Meter A",
                Data = new { MeterId = "meter-001", Name = "Fiscal Meter A", AssetId = "asset-001", Type = "Fiscal" },
                CreatedBy = createdBy
            },
            new SeedData
            {
                Id = "meter-002",
                Type = SeedDataType.Meter,
                Name = "Test Meter B",
                Data = new { MeterId = "meter-002", Name = "Test Meter B", WellId = "well-001", Type = "Test" },
                CreatedBy = createdBy
            }
        };
    }

    private List<SeedData> GenerateLabReferenceData(string createdBy)
    {
        return new List<SeedData>
        {
            new SeedData
            {
                Id = "lab-ref-001",
                Type = SeedDataType.LabReference,
                Name = "PVT Standard",
                Data = new { ReferenceId = "lab-ref-001", Name = "PVT Standard", Type = "PVT", Description = "Standard PVT test method" },
                CreatedBy = createdBy
            },
            new SeedData
            {
                Id = "lab-ref-002",
                Type = SeedDataType.LabReference,
                Name = "BS&W Standard",
                Data = new { ReferenceId = "lab-ref-002", Name = "BS&W Standard", Type = "BS&W", Description = "Standard BS&W test method" },
                CreatedBy = createdBy
            }
        };
    }

    private List<SeedData> GenerateUserData(string createdBy)
    {
        return new List<SeedData>
        {
            new SeedData
            {
                Id = "user-001",
                Type = SeedDataType.User,
                Name = "Demo Operator",
                Data = new { UserId = "user-001", Username = "demo.operator", Email = "demo.operator@deltagrid.ng", Role = "ControlRoomOperator" },
                CreatedBy = createdBy
            },
            new SeedData
            {
                Id = "user-002",
                Type = SeedDataType.User,
                Name = "Demo Engineer",
                Data = new { UserId = "user-002", Username = "demo.engineer", Email = "demo.engineer@deltagrid.ng", Role = "ProductionEngineer" },
                CreatedBy = createdBy
            }
        };
    }

    private List<SeedData> GenerateRoleData(string createdBy)
    {
        return new List<SeedData>
        {
            new SeedData
            {
                Id = "role-001",
                Type = SeedDataType.Role,
                Name = "ControlRoomOperator",
                Data = new { RoleId = "role-001", Name = "ControlRoomOperator", Permissions = new[] { "view.wells", "view.alarms" } },
                CreatedBy = createdBy
            },
            new SeedData
            {
                Id = "role-002",
                Type = SeedDataType.Role,
                Name = "ProductionEngineer",
                Data = new { RoleId = "role-002", Name = "ProductionEngineer", Permissions = new[] { "view.wells", "edit.wells", "view.allocation" } },
                CreatedBy = createdBy
            }
        };
    }

    private int GetMinimumCount(SeedDataType type)
    {
        return type switch
        {
            SeedDataType.Tenant => 1,
            SeedDataType.Asset => 2,
            SeedDataType.Well => 3,
            SeedDataType.Meter => 2,
            SeedDataType.LabReference => 2,
            SeedDataType.User => 2,
            SeedDataType.Role => 2,
            _ => 0
        };
    }
}

/// <summary>
/// Seed data repository interface
/// </summary>
public interface ISeedDataRepository
{
    Task SaveSeedDataAsync(SeedData data, CancellationToken ct = default);
    Task<int> GetSeedDataCountAsync(SeedDataType type, CancellationToken ct = default);
    Task ClearSeedDataAsync(SeedDataType type, CancellationToken ct = default);
    Task<List<SeedData>> GetSeedDataAsync(SeedDataType type, CancellationToken ct = default);
}

