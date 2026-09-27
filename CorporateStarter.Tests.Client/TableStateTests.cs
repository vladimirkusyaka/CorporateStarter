using System.Text.Json;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using Xunit;

namespace CorporateStarter.Tests.Client;

public sealed class TableStateTests
{
    private enum Column { Name, Status }

    [Fact]
    public void Selection_uses_display_order_and_drops_rows_from_previous_page()
    {
        var selection = new TableSelection<int>();
        int[] displayed = [7, 3, 9, 1];
        selection.Select(3, displayed, false);
        selection.Select(1, displayed, true);
        Assert.Equal(new[] { 1, 3, 9 }, selection.SelectedIds.Order().ToArray());
        selection.Select(7, displayed, false, true);
        Assert.Equal(4, selection.SelectedIds.Count);
        selection.RetainVisible([12, 13], selectFirstIfEmpty: true);
        Assert.Equal(12, Assert.Single(selection.SelectedIds));
        selection.Select(7, [12, 13], false);
        Assert.Equal(12, Assert.Single(selection.SelectedIds));
    }

    [Fact]
    public void Query_serializes_zero_based_page_and_removes_hidden_status_state()
    {
        var state = new TableQueryState<Column, string>(10) { PageIndex = 2 };
        state.ToggleSort(Column.Status);
        state.SetFilter(Column.Status, "inactive");
        state.SetFilter(Column.Name, "Lead");
        state.RemoveColumn(Column.Status);
        var request = state.BuildRequest(x => x.ToString().ToLowerInvariant(),
            (column, value) => new TableFilter(column.ToString().ToLowerInvariant(), "contains", JsonSerializer.SerializeToElement(value)));
        Assert.Equal(3, request.PageNumber);
        Assert.Empty(request.Sorts);
        Assert.Equal("name", Assert.Single(request.Filters).Field);
        state.PageSize = 20;
        Assert.Equal(0, state.PageIndex);
        state.ApplyPage(2, 21);
        Assert.Equal(1, state.PageIndex);
        Assert.Equal(2, state.TotalPages);
    }
}
