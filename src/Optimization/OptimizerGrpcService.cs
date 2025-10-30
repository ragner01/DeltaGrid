using Grpc.Core;
using IOC.Optimization;
using IOC.Optimization.Inference;
using IOC.Optimization.Rules;

public sealed class OptimizerGrpcService : Optimizer.OptimizerBase
{
    private readonly RulesEngine _rules;
    private readonly OnnxSurrogate _onnx;

    public OptimizerGrpcService(RulesEngine rules, OnnxSurrogate onnx)
    {
        _rules = rules; _onnx = onnx;
    }

    public override Task<OptimizeResponse> Optimize(OptimizeRequest request, ServerCallContext context)
    {
        var window = request.Window.Select(p => (DateTimeOffset.FromUnixTimeMilliseconds(p.TsUnixMs), p.PressurePa, p.TemperatureC, p.FlowM3S, p.ChokePct, p.EspFreqHz));
        var c = (request.Constraints.MinChokePct, request.Constraints.MaxChokePct, request.Constraints.MinPressurePa, request.Constraints.MaxPressurePa, request.Constraints.MinTemperatureC, request.Constraints.MaxTemperatureC);
        var (rChoke, rEsp, rationaleRules) = _rules.Recommend(request.LiftMethod, window, c);
        var last = request.Window.LastOrDefault();
        var feats = new double[] { last.PressurePa, last.TemperatureC, last.FlowM3S, last.ChokePct, last.EspFreqHz, rChoke, rEsp };
        var (mChoke, mEsp, rationaleOnnx) = _onnx.Predict(request.LiftMethod, feats);
        double choke = Math.Clamp((rChoke + mChoke) / 2.0, request.Constraints.MinChokePct, request.Constraints.MaxChokePct);
        double esp = Math.Max(0, (rEsp + mEsp) / 2.0);
        return Task.FromResult(new OptimizeResponse
        {
            WellId = request.WellId,
            LiftMethod = request.LiftMethod,
            RecommendedChokePct = Math.Round(choke, 2),
            RecommendedEspFreqHz = Math.Round(esp, 2),
            Rationale = $"{rationaleRules}; {rationaleOnnx}"
        });
    }
}
