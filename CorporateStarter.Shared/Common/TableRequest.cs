using System.Text.Json;
namespace CorporateStarter.Shared.Common;

public sealed class TableRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public List<TableSort> Sorts { get; set; } = [];
    public List<TableFilter> Filters { get; set; } = [];
}
public sealed record TableSort(string Field, string Direction = "asc");
public sealed record TableFilter(string Field, string Operator, JsonElement? Value = null, JsonElement[]? Values = null);
public sealed class TableFindRequest
{
    public TableRequest Query { get; set; } = new();
    public string Text { get; set; } = "";
    public Guid? AfterId { get; set; }
    public Guid? LocateId { get; set; }
}
public sealed class TableFindResult
{
    public Guid? Id { get; set; }
    public int? PageNumber { get; set; }
}
public sealed class TableValuesRequest
{
    public TableRequest Query { get; set; } = new();
    public string Field { get; set; } = "";
}
