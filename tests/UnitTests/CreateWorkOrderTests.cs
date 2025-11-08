using FluentAssertions;
using FluentValidation;
using IOC.Application.Work.CreateWorkOrder;
using IOC.Infrastructure.Persistence;
using Xunit;
using System.Threading.Tasks;

namespace IOC.UnitTests;

public class CreateWorkOrderTests
{
    [Fact]
    public async Task Creates_WorkOrder_When_Valid()
    {
        var validator = new CreateWorkOrderValidator();
        var repo = new InMemoryWorkOrderRepository();
        var handler = new CreateWorkOrderHandler(validator, repo);

        var cmd = new CreateWorkOrderCommand("Title", "Desc", "site-1", "asset-1");
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Title");
    }
}
