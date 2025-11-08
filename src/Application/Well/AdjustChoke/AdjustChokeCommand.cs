using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Well;
using IOC.Application.Common.Outbox;
using IOC.Application.Well;

namespace IOC.Application.Well.AdjustChoke;

public sealed record AdjustChokeCommand(Guid WellId, double NewPercent, string? AssetId) : IRequest<Result>;

public sealed class AdjustChokeValidator : AbstractValidator<AdjustChokeCommand>
{
    public AdjustChokeValidator()
    {
        RuleFor(x => x.WellId).NotEmpty();
        RuleFor(x => x.NewPercent).InclusiveBetween(0, 100);
    }
}

public sealed class AdjustChokeHandler : IRequestHandler<AdjustChokeCommand, Result>
{
    private readonly IWellRepository _repo;
    private readonly IValidator<AdjustChokeCommand> _validator;
    private readonly IAuthorizationService _auth;
    private readonly IOutboxStore _outbox;

    public AdjustChokeHandler(IWellRepository repo, IValidator<AdjustChokeCommand> validator, IAuthorizationService auth, IOutboxStore outbox)
    {
        _repo = repo; _validator = validator; _auth = auth; _outbox = outbox;
    }

    public async Task<Result> Handle(AdjustChokeCommand request, CancellationToken cancellationToken)
    {
        var vr = await _validator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid) return Result.Failure(string.Join("; ", vr.Errors.Select(e => e.ErrorMessage)));

        var well = await _repo.GetAsync(request.WellId, cancellationToken);
        if (well is null) return Result.Failure("Well not found");

        // Authorization by asset (if provided)
        var can = await _auth.AuthorizeAsync(new System.Security.Claims.ClaimsPrincipal(), null, "TenantScoped");
        if (!can.Succeeded) return Result.Failure("Forbidden");

        var res = well.SetChoke(request.NewPercent);
        if (!res.IsSuccess) return res;

        await _repo.SaveAsync(well, cancellationToken);
        foreach (var e in well.DomainEvents) await _outbox.EnqueueAsync(e, cancellationToken);
        well.ClearDomainEvents();
        return Result.Success();
    }
}
