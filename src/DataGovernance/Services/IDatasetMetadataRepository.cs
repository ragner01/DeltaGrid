using IOC.DataGovernance.Models;

namespace IOC.DataGovernance.Services;

public interface IDatasetMetadataRepository
{
    Task<List<DatasetMetadata>> GetDatasetsByOwnerAsync(string owner, CancellationToken ct = default);
    Task<DatasetMetadata?> GetDatasetAsync(string datasetId, CancellationToken ct = default);
    Task SaveDatasetAsync(DatasetMetadata dataset, CancellationToken ct = default);
}
