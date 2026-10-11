using CorporateStarter.Client.Core.Serialization;
using System.Text.Json;
using Xunit;
namespace CorporateStarter.Tests.Client;

public sealed class JsonDisplayTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Empty_details_are_not_an_error(string? value)
    { Assert.True(JsonDisplay.TryFormat(value, out var text)); Assert.Empty(text); }
    [Fact]
    public void Invalid_details_are_reported_without_throwing()
    { Assert.False(JsonDisplay.TryFormat("{", out var text)); Assert.Empty(text); }
    [Theory]
    [InlineData("{\"value\":\"<b>text</b>\",\"number\":42}")]
    [InlineData("[1,null,true]")]
    [InlineData("null")]
    public void Formatting_preserves_json_values(string value)
    {
        Assert.True(JsonDisplay.TryFormat(value, out var formatted));
        using var before = JsonDocument.Parse(value); using var after = JsonDocument.Parse(formatted);
        Assert.True(JsonElement.DeepEquals(before.RootElement, after.RootElement));
        Assert.DoesNotContain("<b>", formatted);
    }
}