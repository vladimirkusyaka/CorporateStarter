using System.Globalization;
using System.Resources;
namespace CorporateStarter.Shared.Localization;

// Register per client/circuit; never change process-wide DefaultThreadCurrentCulture.
public sealed class UiText
{
    private static readonly ResourceManager Resources = new("CorporateStarter.Shared.Localization.Strings", typeof(UiText).Assembly);
    public static readonly string[] Supported = ["en-US", "de-DE", "ru-RU"];
    public string Language { get; private set; } = "en-US";
    public CultureInfo Culture => CultureInfo.GetCultureInfo(Language);
    public event Action? Changed;
    public static bool IsSupported(string? value) => value is "en-US" or "de-DE" or "ru-RU";
    public static string Match(string? language) => language?.Split('-')[0].ToLowerInvariant() switch
    { "de" => "de-DE", "ru" => "ru-RU", _ => "en-US" };
    public void Select(string language)
    {
        if (!IsSupported(language)) throw new ArgumentException("Unsupported language.", nameof(language));
        if (Language == language) return;
        Language = language; Changed?.Invoke();
    }
    public string this[string key] => Get(key, Culture);
    public string? Text(string? key) => key is null ? null : this[key];
    public string Format(string key, params object?[] args) => string.Format(Culture, this[key], args);
    public static string Get(string key, CultureInfo culture) => Resources.GetString(key.Length > 0 && char.IsLower(key[0]) ? "term." + key : key, culture) ?? key;
}
