namespace IOC.Optimization;

using Grpc.Core;
using IOC.Optimization.Inference;
using IOC.Optimization.Rules;

/// <summary>
/// gRPC service for optimization requests
/// </summary>
public sealed class OptimizerGrpcService : Optimizer.OptimizerBase
{
    private readonly RulesEngine rulesEngine;
    private readonly OnnxSurrogate onnxSurrogate;

    /// <summary>
    /// Initializes a new instance of the OptimizerGrpcService class
    /// </summary>
    public OptimizerGrpcService(RulesEngine rules, OnnxSurrogate onnx)
    {
        this.rulesEngine = rules;
        this.onnxSurrogate = onnx;
    }

    public override Task<OptimizeResponse> Optimize(OptimizeRequest request, ServerCallContext context)
    {
        // Convert gRPC types to tuples expected by RulesEngine
        IEnumerable<(DateTimeOffset ts, double pressurePa, double temperatureC, double flowM3s, double chokePct, double espFreqHz)> window = request.Window.Select(p =>
            (
                ts: DateTimeOffset.FromUnixTimeMilliseconds(p.TsUnixMs),
                pressurePa: p.PressurePa,
                temperatureC: p.TemperatureC,
                flowM3s: p.FlowM3S,
                chokePct: p.ChokePct,
                espFreqHz: p.EspFreqHz
            ));
        (double minChoke, double maxChoke, double minP, double maxP, double minT, double maxT) c = (
            request.Constraints.MinChokePct,
            request.Constraints.MaxChokePct,
            request.Constraints.MinPressurePa,
            request.Constraints.MaxPressurePa,
            request.Constraints.MinTemperatureC,
            request.Constraints.MaxTemperatureC);
        (double chokePct, double espFreqHz, string rationale) result = this.rulesEngine.Recommend(request.LiftMethod, window, c);
        double rChoke = result.chokePct;
        double rEsp = result.espFreqHz;
        string rationaleRules = result.rationale;
        TelemetryPoint? last = request.Window.LastOrDefault();
        if (last == null)
        {
            return Task.FromResult(new OptimizeResponse
            {
                WellId = request.WellId,
                LiftMethod = request.LiftMethod,
                RecommendedChokePct = 0,
                RecommendedEspFreqHz = 0,
                Rationale = "Error: Window cannot be empty",
            });
        }

        double[] feats = { last.PressurePa, last.TemperatureC, last.FlowM3S, last.ChokePct, last.EspFreqHz, rChoke, rEsp };
        (double, double, string) onnxResult = this.onnxSurrogate.Predict(request.LiftMethod, feats);
        double mChoke = onnxResult.Item1;
        double mEsp = onnxResult.Item2;
        string rationaleOnnx = onnxResult.Item3;
        double choke = Math.Clamp((rChoke + mChoke) / 2.0, request.Constraints.MinChokePct, request.Constraints.MaxChokePct);
        double esp = Math.Max(0, (rEsp + mEsp) / 2.0);
        return Task.FromResult(new OptimizeResponse
        {
            WellId = request.WellId,
            LiftMethod = request.LiftMethod,
            RecommendedChokePct = Math.Round(choke, 2),
            RecommendedEspFreqHz = Math.Round(esp, 2),
            Rationale = $"{rationaleRules}; {rationaleOnnx}",
        });
    }
}
