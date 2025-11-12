using FluentValidation;
using IOC.Optimization;

namespace IOC.Optimization.Validators;

/// <summary>
/// Validator for OptimizeRequest to prevent injection and DoS attacks
/// </summary>
public sealed class OptimizeRequestValidator : AbstractValidator<OptimizeRequest>
{
    public OptimizeRequestValidator()
    {
        RuleFor(x => x.WellId)
            .NotEmpty()
            .WithMessage("WellId cannot be empty")
            .Matches(@"^[a-zA-Z0-9\-_]+$")
            .WithMessage("WellId must contain only alphanumeric characters, hyphens, and underscores")
            .MaximumLength(100)
            .WithMessage("WellId cannot exceed 100 characters");
        
        RuleFor(x => x.LiftMethod)
            .NotEmpty()
            .WithMessage("LiftMethod cannot be empty")
            .Must(m => m == "GasLift" || m == "ESP")
            .WithMessage("LiftMethod must be either 'GasLift' or 'ESP'");
        
        RuleFor(x => x.Window)
            .NotEmpty()
            .WithMessage("Window cannot be empty")
            .Must(w => w.Count > 0 && w.Count <= 1000)
            .WithMessage("Window size must be between 1 and 1000 points");
        
        RuleFor(x => x.Constraints)
            .NotNull()
            .WithMessage("Constraints cannot be null");
        
        RuleFor(x => x.Constraints.MinChokePct)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MinChokePct must be >= 0")
            .LessThanOrEqualTo(100)
            .WithMessage("MinChokePct must be <= 100")
            .LessThan(x => x.Constraints.MaxChokePct)
            .WithMessage("MinChokePct must be less than MaxChokePct");
        
        RuleFor(x => x.Constraints.MaxChokePct)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxChokePct must be >= 0")
            .LessThanOrEqualTo(100)
            .WithMessage("MaxChokePct must be <= 100");
        
        RuleFor(x => x.Constraints.MinPressurePa)
            .LessThan(x => x.Constraints.MaxPressurePa)
            .WithMessage("MinPressurePa must be less than MaxPressurePa");
        
        RuleFor(x => x.Constraints.MinTemperatureC)
            .LessThan(x => x.Constraints.MaxTemperatureC)
            .WithMessage("MinTemperatureC must be less than MaxTemperatureC");
        
        // Validate each telemetry point
        RuleForEach(x => x.Window)
            .ChildRules(point =>
            {
                point.RuleFor(p => p.PressurePa)
                    .InclusiveBetween(-1000000, 100000000)
                    .WithMessage("PressurePa must be between -1,000,000 and 100,000,000");
                
                point.RuleFor(p => p.TemperatureC)
                    .InclusiveBetween(-50, 500)
                    .WithMessage("TemperatureC must be between -50 and 500");
                
                point.RuleFor(p => p.FlowM3S)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("FlowM3S must be >= 0");
                
                point.RuleFor(p => p.ChokePct)
                    .InclusiveBetween(0, 100)
                    .WithMessage("ChokePct must be between 0 and 100");
                
                point.RuleFor(p => p.EspFreqHz)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("EspFreqHz must be >= 0");
            });
    }
}

