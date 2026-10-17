namespace CorporateStarter.Infrastructure.Persistence;

/// <summary>Human-readable snapshot label, separate from the immutable primary key.</summary>
public static class AuditRecordName
{
    public static string? FromValues(string entityName, IReadOnlyDictionary<string, object?> values)
    {
        string? Text(string key) => values.TryGetValue(key, out var value) && value is string text
            && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;
        return entityName switch
        {
            "Person" => NullIfEmpty(string.Join(" ", new[] { Text("FirstName"), Text("LastName") }
                .Where(x => x is not null))),
            "User" => Text("DisplayName") ?? Text("Login"),
            "Country" or "City" or "Company" or "Position" or "Role" or "Permission"
                or "ApplicationPreferences" => Text("Name"),
            _ => null // Unknown entities and composite-key relationships keep their record ID.
        };
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
