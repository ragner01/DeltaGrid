namespace IOC.Optimization.Inference;

using System.Security.Cryptography;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

public sealed class OnnxSurrogate : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _modelPath;

    public OnnxSurrogate(string modelPath, string expectedSha256)
    {
        this._modelPath = modelPath;
        VerifyChecksum(modelPath, expectedSha256);
        this._session = new InferenceSession(modelPath);
    }

    public (double chokePct, double espFreqHz, string rationale) Predict(string liftMethod, double[] features)
    {
        _ = liftMethod; // Parameter reserved for future use
        var input = new DenseTensor<float>(features.Select(f => (float)f).ToArray(), new[] { 1, features.Length });
        var name = this._session.InputMetadata.Keys.First();
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(name, input) };
        using var results = this._session.Run(inputs);
        var output = results[0].AsEnumerable<float>().ToArray();
        double choke = Math.Round(output.ElementAtOrDefault(0), 2);
        double esp = Math.Round(output.ElementAtOrDefault(1), 2);
        var rationale = $"onnx:features={features.Length}";
        return (choke, esp, rationale);
    }

    private static void VerifyChecksum(string path, string expectedSha256)
    {
        using var fs = File.OpenRead(path);
        var hash = Convert.ToHexString(SHA256.HashData(fs)).ToLowerInvariant();
        if (!string.Equals(hash, expectedSha256.ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Model checksum verification failed");
        }
    }

    public void Dispose()
    {
        this._session.Dispose();
    }
}
