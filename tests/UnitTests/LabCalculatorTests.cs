using FluentAssertions;
using IOC.Application.Lab;
using IOC.Core.Domain.Lab;
using IOC.Infrastructure.Persistence;
using MediatR;
using Xunit;

namespace IOC.UnitTests;

public class LabCalculatorTests
{
    [Fact]
    public void Calculators_Adjust_As_Expected()
    {
        PropertyCalculators.ShrinkageFactor(35).Should().BeApproximately(0.992, 1e-6);
        PropertyCalculators.ShrinkageFactor(45).Should().BeApproximately(0.985, 1e-6);
        PropertyCalculators.AdjustedGOR(1000, 0.2).Should().BeApproximately(960, 1e-6);
        PropertyCalculators.AdjustedWaterCut(0.3, 50000).Should().BeApproximately(0.375, 1e-6);
    }

    [Fact]
    public async Task Recording_New_Result_Closes_Prior_Validity()
    {
        var repo = new InMemoryLabRepository();
        var sink = new TestLabPropertySink();
        var handlers = new Commands.Handlers(repo, sink, new TestPdfSigner());

        var sampleId = "S-1";
        await handlers.Handle(new Commands.PlanSampleCommand(sampleId, "W-1", DateTimeOffset.UtcNow, "BAR-1"), CancellationToken.None);
        await handlers.Handle(new Commands.RecordLabResultCommand(sampleId, "ASTM-Dxxx v1", 35, 800, 0.2, 20000, 10, "cert://url1", "lab"), CancellationToken.None);
        var first = await repo.GetActiveResultAsync(sampleId, CancellationToken.None);
        first.Should().NotBeNull();
        first!.EffectiveTo.Should().BeNull();

        await Task.Delay(5);
        await handlers.Handle(new Commands.RecordLabResultCommand(sampleId, "ASTM-Dxxx v2", 36, 820, 0.22, 21000, 11, "cert://url2", "lab"), CancellationToken.None);
        var second = await repo.GetActiveResultAsync(sampleId, CancellationToken.None);
        second.Should().NotBeNull();
        second!.MethodVersion.Should().Be("ASTM-Dxxx v2");
    }
}

file sealed class TestLabPropertySink : ILabPropertySink
{
    public Task PushToAllocationAsync(string sourceId, double? api, double? gor, double? wc, CancellationToken ct) => Task.CompletedTask;
    public Task PushToOptimizationAsync(string sourceId, double? api, double? gor, double? viscosity, CancellationToken ct) => Task.CompletedTask;
}

file sealed class TestPdfSigner : IPdfSigner
{
    public (string Algo, string Signature) Sign(string certificateUrl) => ("none", $"sig::{certificateUrl}");
}
