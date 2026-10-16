using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.UI.Components.Layout;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class PermissionAccessTests
{
    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(false, true, true, false, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, true, false, true, false)]
    [InlineData(true, false, true, false, true)]
    [InlineData(true, true, true, true, true)]
    public void Status_rights_are_directional(bool view, bool delete, bool restore, bool deactivateAllowed, bool restoreAllowed)
    {
        var caps = new TableCapabilities(true, true, delete, view, restore);
        Assert.Equal(deactivateAllowed, caps.CanChangeStatus(true));
        Assert.Equal(restoreAllowed, caps.CanChangeStatus(false));
        caps.RequireStatusChange(true, true); caps.RequireStatusChange(false, false);
        if (!deactivateAllowed) Assert.Throws<UnauthorizedAccessException>(() => caps.RequireStatusChange(true, false));
        if (!restoreAllowed) Assert.Throws<UnauthorizedAccessException>(() => caps.RequireStatusChange(false, true));
    }

    [Fact]
    public void Groups_and_direct_routes_use_the_same_permission_catalog()
    {
        Assert.Empty(NavigationCatalog.VisibleGroups(_ => false));
        var groups = NavigationCatalog.VisibleGroups(code => code is "cities.read" or "audit.read").ToArray();
        Assert.Equal(new[] { "Master Data", "Audit" }, groups.Select(g => g.Title));
        Assert.Equal("/cities", Assert.Single(groups[0].Items).Path);
        Assert.Equal(2, groups[1].Items.Count);
        Assert.Equal("cities.read", NavigationCatalog.RequiredPermission("CITIES/?page=2"));
        Assert.Equal("cities.read", NavigationCatalog.RequiredPermission("cities/123"));
        Assert.Null(NavigationCatalog.RequiredPermission("profile"));
    }

    [Fact]
    public void Old_permissions_and_late_profiles_cannot_cross_session_boundaries()
    {
        var state = State(); using var permissions = new ClientPermissions(state);
        var start = state.Current;
        permissions.Apply(Profile(start.UserId!.Value, "users.read"), start);
        Assert.True(permissions.Has("users.read"));
        state.TryTransition(state.Current.Revision, ClientAuthStatus.Anonymous, null, ClientSessionInvalidationReason.SignedOut);
        Assert.False(permissions.IsLoaded); Assert.False(permissions.Has("users.read"));
        state.TryTransition(state.Current.Revision, ClientAuthStatus.Revalidating);
        state.TryTransition(state.Current.Revision, ClientAuthStatus.Authenticated, Guid.NewGuid());
        permissions.Apply(Profile(start.UserId.Value, "users.read"), start);
        Assert.False(permissions.IsLoaded);
        permissions.Apply(Profile(state.Current.UserId!.Value, "cities.read"), state.Current);
        Assert.True(permissions.Has("cities.read")); Assert.False(permissions.Has("users.read"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Menu_hides_empty_groups_in_both_modes(bool expanded)
    {
        var state = State(); using var permissions = new ClientPermissions(state);
        permissions.Apply(Profile(state.Current.UserId!.Value, "cities.read"), state.Current);
        await using var services = TableComponentTests.CreateServices(s => s.AddSingleton(permissions));
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<PermissionNavigation>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { ["Expanded"] = expanded }))).ToHtmlString());
        Assert.Contains("Master Data", html);
        Assert.DoesNotContain("Directory", html); Assert.DoesNotContain("Audit", html);
        Assert.DoesNotContain("/users", html); Assert.DoesNotContain("/countries", html);
        if (expanded) Assert.Contains("/cities", html);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Direct_route_does_not_instantiate_denied_page(bool allowed)
    {
        var state = State(); using var permissions = new ClientPermissions(state);
        permissions.Apply(Profile(state.Current.UserId!.Value, allowed ? "positions.read" : "cities.read"), state.Current);
        await using var services = TableComponentTests.CreateServices(s => s.AddSingleton(permissions));
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var invoked = false;
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<PermissionContent>(ParameterView.FromDictionary(new Dictionary<string, object?>
            { ["ChildContent"] = (RenderFragment)(b => { invoked = true; b.AddContent(0, "protected-records"); }) }))).ToHtmlString());
        Assert.Equal(allowed, invoked);
        Assert.Equal(allowed, html.Contains("protected-records"));
        Assert.Equal(!allowed, html.Contains("do not have permission"));
    }

    private static ClientAuthStateStore State()
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, Guid.NewGuid()); return state;
    }
    private static UserProfileDto Profile(Guid id, string permission) => new()
    { Id = id, Roles = [new() { Permissions = [new() { Code = permission }] }] };
}
