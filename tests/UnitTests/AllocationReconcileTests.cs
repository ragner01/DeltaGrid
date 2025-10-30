using FluentAssertions;
using IOC.Application.Allocation;
using IOC.Application.Allocation.Reconcile;
using IOC.Application.Allocation.RunAllocation;
using IOC.Core.Domain.Allocation;
using IOC.Infrastructure.Persistence;

namespace IOC.UnitTests;

public class AllocationReconcileTests
{
    [Fact]
    public async Task Reconcile_Variance_Within_Threshold()
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        InMemoryBatteryRepository.Seed("bat-x", "site-1", "asset-1", new[] { "a", "b" });
        InMemoryMeterReadingRepository.Seed(new BatteryMeasurement("bat-x", day, 20.0, 0.0, 0.0));
        InMemoryWellTestRepository.Seed(new WellTest("a", day, 10, 0, 0));
        InMemoryWellTestRepository.Seed(new WellTest("b", day, 10, 0, 0));

        var runHandler = new RunAllocationHandler(new InMemoryBatteryRepository(), new InMemoryMeterReadingRepository(), new InMemoryWellTestRepository(), new InMemoryAllocationRunRepository());
        var run = await runHandler.Handle(new RunAllocationCommand(day, "bat-x", "ProportionalByTest"), CancellationToken.None);
        run.IsSuccess.Should().BeTrue();

        var reconHandler = new ReconcileAllocationHandler(new InMemoryMeterReadingRepository(), new InMemoryAllocationRunRepository(), new InMemoryAllocationRunRepository());
        var recon = await reconHandler.Handle(new ReconcileAllocationCommand(day, "bat-x", 0.1), CancellationToken.None);
        recon.IsSuccess.Should().BeTrue();
        recon.Value!.WithinThreshold.Should().BeTrue();
    }
}
