using FluentValidation;
using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Allocation;

namespace IOC.Application.Allocation.RunAllocation;

public sealed record RunAllocationCommand(DateOnly Day, string BatteryId, string Method) : IRequest<Result<Guid>>;

public sealed class RunAllocationValidator : AbstractValidator<RunAllocationCommand>
{
    public RunAllocationValidator()
    {
        RuleFor(x => x.BatteryId).NotEmpty();
        RuleFor(x => x.Day).NotEmpty();
        RuleFor(x => x.Method).NotEmpty();
    }
}

public sealed class RunAllocationHandler : IRequestHandler<RunAllocationCommand, Result<Guid>>
{
    private readonly IBatteryRepository _batteries;
    private readonly IMeterReadingRepository _meters;
    private readonly IWellTestRepository _tests;
    private readonly IAllocationRunRepository _runs;

    public RunAllocationHandler(IBatteryRepository batteries, IMeterReadingRepository meters, IWellTestRepository tests, IAllocationRunRepository runs)
    {
        _batteries = batteries; _meters = meters; _tests = tests; _runs = runs;
    }

    public async Task<Result<Guid>> Handle(RunAllocationCommand request, CancellationToken cancellationToken)
    {
        var battery = await _batteries.GetAsync(request.BatteryId, cancellationToken);
        if (battery is null) return Result<Guid>.Failure("Battery not found");

        var meas = await _meters.GetBatteryMeasurementAsync(request.BatteryId, request.Day, cancellationToken);
        if (meas is null) return Result<Guid>.Failure("Measurement not found");

        var tests = await _tests.GetTestsAsync(battery.WellIds, request.Day, cancellationToken);

        IAllocationStrategy strategy = request.Method switch
        {
            "ProportionalByTest" => new ProportionalByTestStrategy(),
            _ => new ProportionalByTestStrategy()
        };

        var version = await _runs.GetNextVersionAsync(request.BatteryId, request.Day, cancellationToken);
        var results = strategy.Allocate(request.Day, battery, meas, tests, version);
        var run = new AllocationRun(request.Day, request.BatteryId, strategy.Method, version, results);
        await _runs.SaveAsync(run, cancellationToken);
        return Result<Guid>.Success(run.RunId);
    }
}
