using System.Security.Claims;

namespace OpsConsole.Services;

public interface IWidget
{
    string Id { get; }
    string Title { get; }
    string RequiredRole { get; }
}

public sealed class WidgetRegistry
{
    private readonly List<IWidget> _widgets = new();
    public void Register(IWidget widget) => _widgets.Add(widget);
    public IEnumerable<IWidget> GetForUser(ClaimsPrincipal user)
    {
        return _widgets.Where(w => string.IsNullOrWhiteSpace(w.RequiredRole) || user.IsInRole(w.RequiredRole));
    }
}
