using CorporateStarter.Shared.Localization;
using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Resources;
namespace CorporateStarter.Client.UI.Localization;

public sealed class UiMudLocalization(UiText text) : ILocalizationInterceptor
{
    private static readonly ResourceManager English = new("MudBlazor.Resources.LanguageResource", typeof(MudLocalizer).Assembly);
    public LocalizedString Handle(string key, params object[] arguments)
    {
        var template = English.GetString(key, System.Globalization.CultureInfo.InvariantCulture);
        return template is null ? new(key, key, true) : new(key, text.Format(template, arguments));
    }
}
