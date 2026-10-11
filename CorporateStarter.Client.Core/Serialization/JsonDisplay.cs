using System.Text.Json;
namespace CorporateStarter.Client.Core.Serialization;

public static class JsonDisplay
{
    public static bool TryFormat(string? json, out string text)
    {
        text = "";
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            using var document = JsonDocument.Parse(json);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) document.WriteTo(writer);
            text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            return true;
        }
        catch (JsonException) { return false; }
    }
}
