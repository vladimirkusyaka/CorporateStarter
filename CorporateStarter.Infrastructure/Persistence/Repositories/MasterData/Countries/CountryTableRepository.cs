using System.Data;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.EntityFrameworkCore;

// SQL fragments are server-owned; SqlTableQuery validates identifiers and parameterizes every user value.
#pragma warning disable EF1002

namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Countries;

public sealed class CountryTableRepository(AppDbContext db, ICountryAccess access) : ICountryTableRepository
{
    private const string Projection = "\"Id\", \"Code\", \"Name\", \"NativeName\", \"PhoneCode\", \"IsActive\"";
    private static readonly Dictionary<string, SqlTableColumn> Columns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["code"] = new("\"Code\""),
        ["name"] = new("\"Name\""),
        ["nativeName"] = new("\"NativeName\""),
        ["phoneCode"] = new("\"PhoneCode\""),
        ["isActive"] = new("\"IsActive\"", true)
    };
    private SqlTableQuery Build(TableRequest request) => new(request, Columns,
        new HashSet<string>(access.Capabilities.ViewInactive ? [] : ["isActive"], StringComparer.OrdinalIgnoreCase),
        access.Capabilities.ViewInactive ? "TRUE" : "\"IsActive\" = TRUE", "lower(\"Name\") asc", "\"Id\" asc");

    public async Task<PagedResult<CountryListItemDto>> QueryAsync(TableRequest request, CancellationToken ct)
    {
        var query = Build(request);
        // Count and rows are read from the same snapshot.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var total = await db.Database.SqlQueryRaw<int>($"SELECT count(*)::int AS \"Value\" FROM \"Countries\" WHERE {query.Where}", query.Parameters).SingleAsync(ct);
        var page = Math.Min(request.PageNumber, Math.Max(1, (int)Math.Ceiling(total / (double)request.PageSize)));
        var limit = query.Parameter(request.PageSize);
        var offset = query.Parameter((page - 1) * request.PageSize);
        var rows = await db.Database.SqlQueryRaw<CountryListItemDto>(
            $"SELECT {Projection} FROM \"Countries\" WHERE {query.Where} ORDER BY {query.Order} LIMIT {limit} OFFSET {offset}", query.Parameters).ToListAsync(ct);
        await transaction.CommitAsync(ct);
        return new() { Items = rows, TotalCount = total, Page = page, PageSize = request.PageSize };
    }

    public async Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct)
    {
        if (request.Text is null || request.Text.Length > 200) throw new ArgumentException("Search text is too long.");
        var query = Build(request.Query);
        if (request.LocateId is null && string.IsNullOrWhiteSpace(request.Text)) return new();
        var pageSize = query.Parameter(request.Query.PageSize);
        string match;
        if (request.LocateId is { } id) match = $"\"Id\" = {query.Parameter(id)}";
        else
        {
            var text = query.Parameter(request.Text.Trim());
            match = string.Join(" OR ", Columns.Where(x => !x.Value.Boolean).Select(x =>
                $"strpos(lower(coalesce({x.Value.Sql}, '')), lower({text})) > 0"));
        }
        var after = request.AfterId is { } afterId
            ? $"coalesce((SELECT rn FROM ranked WHERE \"Id\" = {query.Parameter(afterId)}), 0)" : "0";
        var sql = $"""
            WITH ranked AS (
                SELECT {Projection}, row_number() OVER (ORDER BY {query.Order}) AS rn
                FROM "Countries" WHERE {query.Where}
            )
            SELECT "Id", ((rn - 1) / {pageSize} + 1)::int AS "PageNumber"
            FROM ranked WHERE ({match})
            ORDER BY CASE WHEN rn > {after} THEN 0 ELSE 1 END, rn LIMIT 1
            """;
        return (await db.Database.SqlQueryRaw<TableFindResult>(sql, query.Parameters).ToListAsync(ct)).FirstOrDefault() ?? new();
    }

    public async Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct)
    {
        var query = Build(request.Query); // Validate all filters even though this endpoint lists all authorized distinct values.
        var column = query.Column(request.Field);
        if (!column.Boolean) throw new ArgumentException("This column uses a text filter.");
        var security = access.Capabilities.ViewInactive ? "TRUE" : "\"IsActive\" = TRUE";
        var sql = $"SELECT DISTINCT CASE WHEN {column.Sql} THEN 'Active' ELSE 'Inactive' END AS \"Value\" FROM \"Countries\" WHERE {security} ORDER BY \"Value\"";
        return await db.Database.SqlQueryRaw<string>(sql).ToArrayAsync(ct);
    }
}
