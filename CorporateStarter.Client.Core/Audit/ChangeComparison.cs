using System.Text.Json;

namespace CorporateStarter.Client.Core.Audit;

public sealed record ChangeValue(string Field, string Before, string After, bool Changed);

public sealed record ChangeComparison(IReadOnlyList<ChangeValue> Rows, bool InvalidJson)
{
    public static ChangeComparison Parse(string? before, string? after)
    {
        try
        {
            var oldValues = Read(before); var newValues = Read(after);

            var rows = oldValues.Keys.Union(newValues.Keys).OrderBy(x => x, StringComparer.Ordinal)

                .Select(key => new ChangeValue(key, oldValues.GetValueOrDefault(key, "\u2014 (not present)"),

                    newValues.GetValueOrDefault(key, "\u2014 (not present)"),

                    oldValues.ContainsKey(key) != newValues.ContainsKey(key) || oldValues.GetValueOrDefault(key) != newValues.GetValueOrDefault(key))).ToArray();

            return new(rows, false);
        }
        catch (JsonException) { return new([], true); }
    }

    private static Dictionary<string, string> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("Expected an object.");

        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in doc.RootElement.EnumerateObject())

            if (!result.TryAdd(property.Name, property.Value.GetRawText())) throw new JsonException("Duplicate field.");

        return result;
    }
}
