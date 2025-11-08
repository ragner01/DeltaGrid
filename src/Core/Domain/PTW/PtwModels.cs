namespace IOC.Core.Domain.PTW;

public enum WorkOrderStatus
{
    Draft,
    Planned,
    InProgress,
    Completed,
    Cancelled
}

public enum PermitType
{
    Hot,
    Cold,
    ConfinedSpace,
    Electrical
}

public enum PermitStatus
{
    Draft,
    PendingApproval,
    Approved,
    Active,
    Suspended,
    Closed,
    Rejected
}

public sealed record IsolationPoint(string Id, string Description, bool Isolated, string Method); // e.g., valve tag, lock, tag

public sealed class Signature
{
    public string UserId { get; }
    public DateTimeOffset SignedAt { get; }
    public string Role { get; }
    public string SignatureHash { get; }

    public Signature(string userId, string role, DateTimeOffset signedAt, string signatureHash)
    {
        UserId = userId; Role = role; SignedAt = signedAt; SignatureHash = signatureHash;
    }
}

public sealed class WorkOrder
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string SiteId { get; private set; }
    public string AssetId { get; private set; }
    public WorkOrderStatus Status { get; private set; } = WorkOrderStatus.Draft;

    public WorkOrder(string title, string description, string siteId, string assetId)
    {
        Title = title; Description = description; SiteId = siteId; AssetId = assetId;
    }

    public void Start() => Status = WorkOrderStatus.InProgress;
    public void Complete() => Status = WorkOrderStatus.Completed;
}

public sealed class Permit
{
    public Guid Id { get; } = Guid.NewGuid();
    public PermitType Type { get; private set; }
    public string SiteId { get; private set; }
    public string AssetId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public PermitStatus Status { get; private set; } = PermitStatus.Draft;
    public List<IsolationPoint> Isolations { get; } = new();
    public List<Signature> Signatures { get; } = new();
    public List<string> Attachments { get; } = new(); // store URIs/hashes

    public string PrevHash { get; private set; } = string.Empty;
    public string ChainHash { get; private set; } = string.Empty;

    public Permit(PermitType type, string siteId, string assetId, Guid workOrderId)
    {
        Type = type; SiteId = siteId; AssetId = assetId; WorkOrderId = workOrderId;
        UpdateChainHash();
    }

    public void AddIsolation(IsolationPoint p) { Isolations.Add(p); UpdateChainHash(); }
    public void AddSignature(Signature s) { Signatures.Add(s); UpdateChainHash(); }
    public void AddAttachment(string uri) { Attachments.Add(uri); UpdateChainHash(); }

    public void Submit() { Status = PermitStatus.PendingApproval; UpdateChainHash(); }
    public void Approve(Signature approver)
    {
        AddSignature(approver);
        Status = PermitStatus.Approved;
        UpdateChainHash();
    }
    public void Activate() { Status = PermitStatus.Active; UpdateChainHash(); }
    public void Close(Signature closer)
    {
        AddSignature(closer);
        Status = PermitStatus.Closed;
        UpdateChainHash();
    }

    public string ComputePayloadHash()
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            Type,
            SiteId,
            AssetId,
            WorkOrderId,
            Status,
            Isolations,
            Signatures,
            Attachments
        });
        var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }

    private void UpdateChainHash()
    {
        var payloadHash = ComputePayloadHash();
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(PrevHash + ":" + payloadHash);
        ChainHash = Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }

    public void AdvanceHashChain()
    {
        PrevHash = ChainHash;
        UpdateChainHash();
    }
}
