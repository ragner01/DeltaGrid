using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Well;
using IOC.Application.Common.Outbox;

namespace IOC.Application.Well.ChangeState;

public sealed record ChangeWellStateCommand(Guid WellId, WellState TargetState, DateTimeOffset At) : IRequest<Result>;

public sealed class ChangeWellStateValidator : AbstractValidator<ChangeWellStateCommand>
{
    public ChangeWellStateValidator()
    {
        RuleFor(x => x.WellId).NotEmpty();
        RuleFor(x => x.TargetState).IsInEnum();
    }
}

public sealed class ChangeWellStateHandler : IRequestHandler<ChangeWellStateCommand, Result>
{
    private readonly IWellRepository _repo;
    private readonly IValidator<ChangeWellStateCommand> _validator;
    private readonly IAuthorizationService _auth;
    private readonly IOutboxStore _outbox;

    public ChangeWellStateHandler(IWellRepository repo, IValidator<ChangeWellStateCommand> validator, IAuthorizationService auth, IOutboxStore outbox)
    {
        _repo = repo; _validator = validator; _auth = auth; _outbox = outbox;
    }

    public async Task<Result> Handle(ChangeWellStateCommand request, CancellationToken cancellationToken)
    {
        var vr = await _validator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid) return Result.Failure(string.Join("; ", vr.Errors.Select(e => e.ErrorMessage)));

        var well = await _repo.GetAsync(request.WellId, cancellationToken);
        if (well is null) return Result.Failure("Well not found");

        var can = await _auth.AuthorizeAsync(new System.Security.Claims.ClaimsPrincipal(), null, "TenantScoped");
        if (!can.Succeeded) return Result.Failure("Forbidden");

        var res = well.ChangeState(request.TargetState, request.At);
        if (!res.IsSuccess) return res;

        await _repo.SaveAsync(well, cancellationToken);
        foreach (var e in well.DomainEvents) await _outbox.EnqueueAsync(e, cancellationToken);
        well.ClearDomainEvents();
        return Result.Success();
    }
}
