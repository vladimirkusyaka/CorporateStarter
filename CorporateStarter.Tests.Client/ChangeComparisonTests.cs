using CorporateStarter.Client.Core.Audit;
using Xunit;

namespace CorporateStarter.Tests.Client;

public sealed class ChangeComparisonTests
{
    [Fact]
    public void Distinguishes_missing_null_unchanged_and_changed_fields()
    {
        var result = ChangeComparison.Parse("{\"Name\":\"Old\",\"Same\":1,\"Removed\":null}", "{\"Name\":\"New\",\"Same\":1,\"Added\":null}");

        Assert.False(result.InvalidJson); Assert.Equal(4, result.Rows.Count);

        Assert.False(result.Rows.Single(x => x.Field == "Same").Changed);

        Assert.Equal(3, result.Rows.Count(x => x.Changed));

        Assert.Equal("null", result.Rows.Single(x => x.Field == "Added").After);

        Assert.Equal("\u2014 (not present)", result.Rows.Single(x => x.Field == "Added").Before);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"a\":1,\"a\":2}")]
    public void Malformed_or_ambiguous_snapshot_does_not_crash(string json) => Assert.True(ChangeComparison.Parse(json, null).InvalidJson);

    [Fact]
    public void Create_and_delete_show_all_fields()
    {
        const string json = "{\"Name\":\"Example\"}";

        Assert.True(Assert.Single(ChangeComparison.Parse(null, json).Rows).Changed);

        Assert.True(Assert.Single(ChangeComparison.Parse(json, null).Rows).Changed);

        Assert.Empty(ChangeComparison.Parse(null, null).Rows);
    }
}