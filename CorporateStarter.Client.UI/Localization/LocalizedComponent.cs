using CorporateStarter.Shared.Localization;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Localization;

public abstract class LocalizedComponent : ComponentBase
{
    [Inject] protected UiText L { get; set; } = default!;
    [CascadingParameter(Name = "UiLanguage")] public string? UiLanguage { get; set; }
}
public abstract class LocalizedLayout : LayoutComponentBase
{
    [Inject] protected UiText L { get; set; } = default!;
    [CascadingParameter(Name = "UiLanguage")] public string? UiLanguage { get; set; }
}
