using System.Text.Json;
using CorporateStarter.Shared.Common;
using Npgsql;

namespace CorporateStarter.Infrastructure.Persistence.Tables;

// All SQL identifiers/expressions come from a server-owned definition. User values are parameters.
public sealed record SqlTableColumn(string Sql, bool Boolean = false);
public sealed class SqlTableQuery
{
    private readonly IReadOnlyDictionary<string, SqlTableColumn> _columns;
    private readonly ISet<string> _forbidden;
    private readonly List<object> _parameters = [];
    public object[] Parameters => _parameters.ToArray();
    public string Where { get; }
    public string Order { get; }
    public TableRequest Request { get; }

    public SqlTableQuery(TableRequest request, IReadOnlyDictionary<string, SqlTableColumn> columns,
        ISet<string> forbidden, string securityPredicate, string defaultOrder, string uniqueOrder)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PageNumber < 1 || request.PageSize is < 1 or > 100 ||
            (long)(request.PageNumber - 1) * request.PageSize > int.MaxValue ||
            request.Sorts is null || request.Filters is null || request.Sorts.Count > 5 || request.Filters.Count > 10)
            throw new ArgumentException("Invalid table request: page size 1–100, up to 5 sorts and 10 filters.");
        Request = request; _columns = columns; _forbidden = forbidden;
        var filters = new List<string> { securityPredicate };
        foreach (var filter in request.Filters)
        {
            if (filter is null) throw new ArgumentException("A filter cannot be null.");
            var column = Column(filter.Field);
            var expression = column.Sql;
            if (filter.Operator == "in")
            {
                if (filter.Values is null || filter.Values.Length > 100) throw new ArgumentException("Invalid filter values.");
                var parameter = column.Boolean
                    ? Parameter(filter.Values.Select(Boolean).ToArray())
                    : Parameter(filter.Values.Select(Text).Select(x => x.ToLowerInvariant()).ToArray());
                filters.Add($"{(column.Boolean ? expression : $"lower(coalesce({expression}, ''))")} = ANY({parameter})");
            }
            else if (filter.Operator == "eq" && column.Boolean)
                filters.Add($"{expression} = {Parameter(Boolean(filter.Value ?? default))}");
            else if (!column.Boolean && filter.Operator is "contains" or "eq")
            {
                var parameter = Parameter(Text(filter.Value ?? default));
                filters.Add(filter.Operator == "contains"
                    ? $"strpos(lower(coalesce({expression}, '')), lower({parameter})) > 0"
                    : $"lower(coalesce({expression}, '')) = lower({parameter})");
            }
            else throw new ArgumentException("This filter operator is not supported for the column.");
        }
        Where = string.Join(" AND ", filters.Select(x => $"({x})"));
        var sorts = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sort in request.Sorts)
        {
            if (sort is null || !used.Add(sort.Field)) throw new ArgumentException("Invalid or duplicate sort.");
            var column = Column(sort.Field);
            if (sort.Direction is not ("asc" or "desc")) throw new ArgumentException("Sort direction must be asc or desc.");
            sorts.Add($"{(column.Boolean ? column.Sql : $"lower(coalesce({column.Sql}, ''))")} {sort.Direction}");
        }
        if (sorts.Count == 0) sorts.Add(defaultOrder);
        sorts.Add(uniqueOrder);
        Order = string.Join(", ", sorts);
    }

    public SqlTableColumn Column(string field)
    {
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("Column is required.");
        if (_forbidden.Contains(field)) throw new UnauthorizedAccessException();
        return _columns.TryGetValue(field, out var column) ? column : throw new ArgumentException("Unknown column.");
    }
    public string Parameter(object value)
    {
        var name = "p" + _parameters.Count;
        _parameters.Add(new NpgsqlParameter(name, value));
        return "@" + name;
    }
    public static string Text(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String || value.GetString()!.Length > 200)
            throw new ArgumentException("Filter text must be a string of at most 200 characters.");
        return value.GetString()!;
    }
    private static bool Boolean(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw new ArgumentException("A boolean filter requires true or false.")
    };
}
