using System.Text.Json;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Common;
using Xunit;

namespace CorporateStarter.Tests.Integration.MasterData;

public sealed class SqlTableQueryTests
{
    private static SqlTableQuery Build(TableRequest request, bool restrictStatus = false) => new(request,
        new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
        { ["name"] = new("\"Name\""), ["isActive"] = new("\"IsActive\"", true) },
        new HashSet<string>(restrictStatus ? ["isActive"] : [], StringComparer.OrdinalIgnoreCase),
        restrictStatus ? "\"IsActive\" = TRUE" : "TRUE", "lower(\"Name\") asc", "\"Id\" asc");

    [Fact]
    public void Untrusted_values_never_become_SQL_and_status_permissions_are_enforced()
    {
        var input = "%' OR TRUE --";
        var query = Build(new() { Filters = [new("name", "contains", JsonSerializer.SerializeToElement(input))] });
        Assert.DoesNotContain(input, query.Where);
        Assert.Contains("@p0", query.Where);
        Assert.Single(query.Parameters);
        Assert.Throws<ArgumentException>(() => Build(new() { Sorts = [new("Name; DROP TABLE Positions")] }));
        Assert.Throws<UnauthorizedAccessException>(() => Build(new() { Sorts = [new("isActive")] }, true));
        Assert.Throws<UnauthorizedAccessException>(() => Build(new() { Filters = [new("isActive", "eq", JsonSerializer.SerializeToElement(false))] }, true));
        Assert.Throws<ArgumentException>(() => Build(new() { PageSize = 101 }));
        Assert.Throws<ArgumentException>(() => Build(new() { PageNumber = int.MaxValue }));
    }
}
