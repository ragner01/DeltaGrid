namespace IOC.Core.Domain.Twin;

using IOC.BuildingBlocks;

public enum TwinLevel
{
    Region,
    Field,
    Facility,
    Train,
    Unit,
    Equipment,
    Tag
}

public sealed class TwinNode : Entity
{
    public string IdPath { get; } // e.g. /Region/R1/Field/F1/Facility/FA/Unit/U-10/Equipment/P-101
    public TwinLevel Level { get; }
    public string Name { get; }
    public bool IsDeleted { get; private set; }
    public int TopologyVersion { get; private set; }

    // Static metadata
    public Dictionary<string, string> Metadata { get; } = new();

    public TwinNode(Guid id, string idPath, TwinLevel level, string name, int topologyVersion) : base(id)
    {
        IdPath = Guard.Against.NullOrWhiteSpace(idPath, nameof(idPath));
        Level = level;
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        TopologyVersion = topologyVersion;
        Guard.Against.InvalidInput(IdPath, nameof(idPath), p => !p.StartsWith('/'), "IdPath must start with '/'");
    }

    public void MarkDeleted(int newVersion)
    {
        IsDeleted = true; TopologyVersion = newVersion;
    }

    public void SetMetadata(string key, string value)
    {
        Metadata[key] = value;
    }
}

public sealed record TwinEdge(string FromIdPath, string ToIdPath, string Relation, int TopologyVersion);
