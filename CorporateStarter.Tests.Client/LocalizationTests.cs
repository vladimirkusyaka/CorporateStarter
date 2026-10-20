using CorporateStarter.Shared.Localization;
using CorporateStarter.Shared.Dtos.Auth;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.UI.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Resources;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData("en-US", "Save", "10/17/2026 1:05:06 PM")]
    [InlineData("de-DE", "Speichern", "17.10.2026 13:05:06")]
    [InlineData("ru-RU", "Сохранить", "17.10.2026 13:05:06")]
    public void Culture_controls_text_and_date_but_not_other_sessions(string language, string save, string date)
    {
        var l = new UiText(); var other = new UiText(); var ambient = CultureInfo.CurrentCulture;
        l.Select(language);
        Assert.Equal(save, l["Save"]);
        var p = new ProfilePreferences(l);
        p.Apply(new() { Language = language });
        Assert.Equal(date, p.Format(new DateTime(2026, 10, 17, 13, 5, 6, DateTimeKind.Utc)));
        p.Apply(new()); // logout resets theme/page size, but keeps the last language
        Assert.Equal(language, l.Language);
        Assert.Equal("en-US", other.Language);
        Assert.Same(ambient, CultureInfo.CurrentCulture);
        Assert.True(new ProfileSettingsDto { Language = language }.IsValid());
    }

    [Theory]
    [InlineData("de-AT", "de-DE")]
    [InlineData("ru", "ru-RU")]
    [InlineData("en-GB", "en-US")]
    [InlineData("fr-FR", "en-US")]
    public void Browser_language_fallback_is_whitelisted(string requested, string expected) => Assert.Equal(expected, UiText.Match(requested));

    [Fact]
    public void Unsupported_profile_language_is_rejected_and_resources_are_complete()
    {
        Assert.False(new ProfileSettingsDto { Language = "fr-FR" }.IsValid());
        Assert.Throws<ArgumentException>(() => new UiText().Select("../ru"));
        var rm = new ResourceManager("CorporateStarter.Shared.Localization.Strings", typeof(UiText).Assembly);
        using var neutral = rm.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        foreach (var culture in new[] { "de", "ru" })
        {
            using var translated = rm.GetResourceSet(CultureInfo.GetCultureInfo(culture), true, false)!;
            foreach (System.Collections.DictionaryEntry entry in neutral)
            {
                var value = translated.GetString((string)entry.Key);
                Assert.False(string.IsNullOrWhiteSpace(value), $"Missing {culture}: {entry.Key}");
                // All composite format placeholders must accept the same arguments.
                _ = string.Format(CultureInfo.InvariantCulture, value!, Enumerable.Repeat<object>("test", 10).ToArray());
            }
        }
    }

    [Fact]
    public void Mud_calendar_and_errors_use_selected_language()
    {
        var l = new UiText(); l.Select("ru-RU"); var mud = new UiMudLocalization(l);
        Assert.Equal("Следующий месяц март", mud.Handle("MudBaseDatePicker_NextMonth", "март").Value);
        Assert.Equal("Некорректная дата или время", mud.Handle("Converter_InvalidDateTime").Value);
        Assert.Equal("Текущий пароль неверен.", l["The current password is incorrect."]);
    }

    [Fact]
    public async Task Cascading_language_change_rerenders_without_recreating_editor_state()
    {
        await using var services = TableComponentTests.CreateServices();
        await using var renderer = new TableComponentTests.TestRenderer(services);
        var host = await renderer.Dispatcher.InvokeAsync(() => renderer.Mount<Host>(new()));
        var instance = host.Child!;
        instance.Draft = "Unsaved user data";
        await renderer.Dispatcher.InvokeAsync(() => host.Switch("de-DE"));
        Assert.Same(instance, host.Child);
        Assert.Equal("Unsaved user data", host.Child!.Draft);
        Assert.Equal("Speichern", host.Child.RenderedLabel);
        Assert.Empty(renderer.Errors);
    }
    public sealed class Probe : LocalizedComponent
    {
        public string Draft = ""; public string? RenderedLabel;
        protected override void BuildRenderTree(RenderTreeBuilder b) { RenderedLabel = L["Save"]; b.AddContent(0, RenderedLabel); b.AddContent(1, Draft); }
    }
    public sealed class Host : ComponentBase
    {
        [Inject] private UiText L { get; set; } = default!;
        public Probe? Child;
        public void Switch(string language) { L.Select(language); StateHasChanged(); }
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<CascadingValue<string>>(0); b.AddAttribute(1, "Name", "UiLanguage"); b.AddAttribute(2, "Value", L.Language);
            b.AddAttribute(3, "ChildContent", (RenderFragment)(c => { c.OpenComponent<Probe>(0); c.AddComponentReferenceCapture(1, value => Child = (Probe)value); c.CloseComponent(); }));
            b.CloseComponent();
        }
    }
}
