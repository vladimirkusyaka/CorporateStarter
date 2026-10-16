namespace CorporateStarter.Client.Core.Auth;

public sealed record NavigationEntry(string Title, string Path, string Permission, string Icon);
public sealed record NavigationGroup(string Title, string Icon, IReadOnlyList<NavigationEntry> Items);

public static class NavigationCatalog
{
    public static IReadOnlyList<NavigationGroup> Groups { get; } = [
        new("Directory", "Business", [
            new("Companies", "/companies", "companies.read", "Apartment"),
            new("Persons", "/persons", "persons.read", "People")]),
        new("Master Data", "Storage", [
            new("Countries", "/countries", "countries.read", "Public"),
            new("Cities", "/cities", "cities.read", "LocationCity"),
            new("Positions", "/positions", "positions.read", "Badge")]),
        new("Security", "Security", [
            new("Users", "/users", "users.read", "ManageAccounts"),
            new("Roles", "/roles", "roles.read", "AdminPanelSettings"),
            new("Permissions", "/permissions", "permissions.read", "Key")]),
        new("Audit", "History", [
            new("Changes", "/changes", "audit.read", "Feed"),
            new("Security events", "/security-events", "audit.read", "Security")])
    ];

    public static IEnumerable<NavigationGroup> VisibleGroups(Func<string, bool> has) => Groups
        .Select(g => g with { Items = g.Items.Where(i => has(i.Permission)).ToArray() })
        .Where(g => g.Items.Count != 0);

    public static string? RequiredPermission(string relativePath)
    {
        var path = "/" + relativePath.Split('?', '#')[0].Trim('/');
        return Groups.SelectMany(g => g.Items).FirstOrDefault(i =>
            path.Equals(i.Path, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(i.Path + "/", StringComparison.OrdinalIgnoreCase))?.Permission;
    }
}
