using FluentValidation;
using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.PTW;

namespace IOC.Application.PTW;

public sealed record CreateWorkOrderCommand(string Title, string Description, string SiteId, string AssetId) : IRequest<Result<Guid>>;
public sealed record CreatePermitCommand(PermitType Type, string SiteId, string AssetId, Guid WorkOrderId) : IRequest<Result<Guid>>;
public sealed record ApprovePermitCommand(Guid PermitId, string UserId, string Role, string SignatureHash) : IRequest<Result>;
public sealed record ActivatePermitCommand(Guid PermitId) : IRequest<Result>;
public sealed record ClosePermitCommand(Guid PermitId, string UserId, string Role, string SignatureHash) : IRequest<Result>;

public interface IPtwRepository
{
    Task<WorkOrder?> GetWorkOrderAsync(Guid id, CancellationToken ct);
    Task<Permit?> GetPermitAsync(Guid id, CancellationToken ct);
    Task SaveWorkOrderAsync(WorkOrder wo, CancellationToken ct);
    Task SavePermitAsync(Permit p, CancellationToken ct);
}

public sealed class CreateWorkOrderValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.AssetId).NotEmpty();
    }
}

public sealed class CreatePermitValidator : AbstractValidator<CreatePermitCommand>
{
    public CreatePermitValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.AssetId).NotEmpty();
    }
}

public sealed class PtwHandlers :
    IRequestHandler<CreateWorkOrderCommand, Result<Guid>>,
    IRequestHandler<CreatePermitCommand, Result<Guid>>,
    IRequestHandler<ApprovePermitCommand, Result>,
    IRequestHandler<ActivatePermitCommand, Result>,
    IRequestHandler<ClosePermitCommand, Result>
{
    private readonly IPtwRepository _repo;
    private readonly IValidator<CreateWorkOrderCommand> _woValidator;
    private readonly IValidator<CreatePermitCommand> _permitValidator;
    private readonly IPermitArchive _archive;

    public PtwHandlers(IPtwRepository repo, IValidator<CreateWorkOrderCommand> woValidator, IValidator<CreatePermitCommand> permitValidator, IPermitArchive archive)
    {
        _repo = repo; _woValidator = woValidator; _permitValidator = permitValidator; _archive = archive;
    }

    public async Task<Result<Guid>> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var vr = await _woValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid) return Result<Guid>.Failure(string.Join("; ", vr.Errors.Select(e => e.ErrorMessage)));
        var wo = new WorkOrder(request.Title, request.Description, request.SiteId, request.AssetId);
        await _repo.SaveWorkOrderAsync(wo, cancellationToken);
        return Result<Guid>.Success(wo.Id);
    }

    public async Task<Result<Guid>> Handle(CreatePermitCommand request, CancellationToken cancellationToken)
    {
        var vr = await _permitValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid) return Result<Guid>.Failure(string.Join("; ", vr.Errors.Select(e => e.ErrorMessage)));
        var wo = await _repo.GetWorkOrderAsync(request.WorkOrderId, cancellationToken);
        if (wo is null) return Result<Guid>.Failure("Work order not found");
        var p = new Permit(request.Type, request.SiteId, request.AssetId, request.WorkOrderId);
        p.Submit();
        await _repo.SavePermitAsync(p, cancellationToken);
        await AppendArchiveAsync(p, nameof(PermitStatus.PendingApproval), cancellationToken);
        p.AdvanceHashChain();
        await _repo.SavePermitAsync(p, cancellationToken);
        return Result<Guid>.Success(p.Id);
    }

    public async Task<Result> Handle(ApprovePermitCommand request, CancellationToken cancellationToken)
    {
        var p = await _repo.GetPermitAsync(request.PermitId, cancellationToken);
        if (p is null) return Result.Failure("Permit not found");
        p.Approve(new Signature(request.UserId, request.Role, DateTimeOffset.UtcNow, request.SignatureHash));
        await _repo.SavePermitAsync(p, cancellationToken);
        await AppendArchiveAsync(p, nameof(PermitStatus.Approved), cancellationToken);
        p.AdvanceHashChain();
        await _repo.SavePermitAsync(p, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(ActivatePermitCommand request, CancellationToken cancellationToken)
    {
        var p = await _repo.GetPermitAsync(request.PermitId, cancellationToken);
        if (p is null) return Result.Failure("Permit not found");
        p.Activate();
        await _repo.SavePermitAsync(p, cancellationToken);
        await AppendArchiveAsync(p, nameof(PermitStatus.Active), cancellationToken);
        p.AdvanceHashChain();
        await _repo.SavePermitAsync(p, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Handle(ClosePermitCommand request, CancellationToken cancellationToken)
    {
        var p = await _repo.GetPermitAsync(request.PermitId, cancellationToken);
        if (p is null) return Result.Failure("Permit not found");
        p.Close(new Signature(request.UserId, request.Role, DateTimeOffset.UtcNow, request.SignatureHash));
        await _repo.SavePermitAsync(p, cancellationToken);
        await AppendArchiveAsync(p, nameof(PermitStatus.Closed), cancellationToken);
        p.AdvanceHashChain();
        await _repo.SavePermitAsync(p, cancellationToken);
        return Result.Success();
    }

    private async Task AppendArchiveAsync(Permit p, string state, CancellationToken ct)
    {
        var record = new PermitArchiveRecord(p.Id, state, p.ComputePayloadHash(), p.PrevHash, p.ChainHash, DateTimeOffset.UtcNow);
        await _archive.AppendAsync(record, ct);
    }
}
