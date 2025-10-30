using FluentValidation;
using MediatR;
using IOC.BuildingBlocks;
using IOC.Core.Domain.Work;

namespace IOC.Application.Work.CreateWorkOrder;

public sealed record CreateWorkOrderCommand(string Title, string Description, string SiteId, string AssetId)
    : IRequest<Result<WorkOrderDto>>;

public sealed class CreateWorkOrderValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.AssetId).NotEmpty();
    }
}

public sealed class WorkOrderDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string AssetId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public interface IWorkOrderRepository
{
    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken);
}

public sealed class CreateWorkOrderHandler : IRequestHandler<CreateWorkOrderCommand, Result<WorkOrderDto>>
{
    private readonly IValidator<CreateWorkOrderCommand> _validator;
    private readonly IWorkOrderRepository _repository;

    public CreateWorkOrderHandler(IValidator<CreateWorkOrderCommand> validator, IWorkOrderRepository repository)
    {
        _validator = validator;
        _repository = repository;
    }

    public async Task<Result<WorkOrderDto>> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<WorkOrderDto>.Failure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var wo = WorkOrder.Create(request.Title, request.Description, request.SiteId, request.AssetId);
        await _repository.AddAsync(wo, cancellationToken);

        var dto = new WorkOrderDto
        {
            Id = wo.Id,
            Title = wo.Title,
            Description = wo.Description,
            SiteId = wo.SiteId,
            AssetId = wo.AssetId,
            CreatedAt = wo.CreatedAt
        };

        return Result<WorkOrderDto>.Success(dto);
    }
}
