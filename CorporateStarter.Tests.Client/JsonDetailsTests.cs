using CorporateStarter.Client.UI.Components.Details;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class JsonDetailsTests
{
    [Theory]
    [InlineData("{\"reason\":\"blocked\"}", "blocked")]
    [InlineData(null, "No additional details.")]
    [InlineData("{", "The stored details are not valid JSON.")]
    public async Task Json_details_renders_values_and_handles_missing_or_invalid_json(string? value, string expected)
    {
        await using var services = TableComponentTests.CreateServices();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<JsonDetails>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = value }));
            return component.ToHtmlString();
        });
        Assert.Contains(expected, html);
    }
}
