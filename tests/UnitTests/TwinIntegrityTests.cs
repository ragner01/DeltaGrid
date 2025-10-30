using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IOC.Application.Twin;
using IOC.Core.Domain.Twin;
using IOC.Infrastructure.Persistence;
using MediatR;
using Xunit;

namespace IOC.UnitTests;

public class TwinIntegrityTests
{
    [Fact]
    public async Task Import_Enforces_Path_Starts_With_Slash()
    {
        var repo = new InMemoryTwinRepository();
        var kpi = new NoOpKpiProvider();
        var h = new Commands.Handlers(repo, kpi);
        var bad = new[] { "Region/R1,Region,R1" };
        var r = await h.Handle(new Commands.ImportHierarchyCommand(bad), CancellationToken.None);
        r.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Import_Creates_Parent_Edges()
    {
        var repo = new InMemoryTwinRepository();
        var kpi = new NoOpKpiProvider();
        var h = new Commands.Handlers(repo, kpi);
        var lines = new[] { "/Region/R1,Region,R1", "/Region/R1/Field/F1,Field,F1" };
        var r = await h.Handle(new Commands.ImportHierarchyCommand(lines), CancellationToken.None);
        r.IsSuccess.Should().BeTrue();
        var edges = await repo.GetEdgesFromAsync("/Region/R1", CancellationToken.None);
        edges.Should().ContainSingle(e => e.ToIdPath == "/Region/R1/Field/F1");
    }
}
