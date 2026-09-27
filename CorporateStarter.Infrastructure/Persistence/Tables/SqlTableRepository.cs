using System.Data;
using CorporateStarter.Shared.Common;
using Microsoft.EntityFrameworkCore;

// SQL fragments are server-owned; SqlTableQuery validates identifiers and parameterizes every user value.
#pragma warning disable EF1002

namespace CorporateStarter.Infrastructure.Persistence.Tables;

public abstract class SqlTableRepository<TItem>(AppDbContext db) where TItem : class
{
    protected abstract string Projection { get; }
    protected abstract string Table { get; }
    protected abstract IReadOnlyDictionary<string, SqlTableColumn> Columns { get; }
    protected virtual string DefaultOrder => "lower(\"Name\") asc";
    protected virtual string UniqueOrder => "\"Id\" asc";
    protected virtual string SecurityPredicate => "TRUE";
    protected virtual ISet<string> ForbiddenColumns => new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private SqlTableQuery Build(TableRequest request) => new(request, Columns,
        ForbiddenColumns, SecurityPredicate, DefaultOrder, UniqueOrder);

    public async Task<PagedResult<TItem>> QueryAsync(TableRequest request, CancellationToken ct)
    {
        var query = Build(request);
        // Count and rows are read from the same snapshot.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var total = await db.Database.SqlQueryRaw<int>($"SELECT count(*)::int AS \"Value\" FROM {Table} WHERE {query.Where}", query.Parameters).SingleAsync(ct);
        var page = Math.Min(request.PageNumber, Math.Max(1, (int)Math.Ceiling(total / (double)request.PageSize)));
        var limit = query.Parameter(request.PageSize);
        var offset = query.Parameter((page - 1) * request.PageSize);
        var rows = await db.Database.SqlQueryRaw<TItem>(
            $"SELECT {Projection} FROM {Table} WHERE {query.Where} ORDER BY {query.Order} LIMIT {limit} OFFSET {offset}", query.Parameters).ToListAsync(ct);
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
            match = string.Join(" OR ", Columns.Where(x => !x.Value.Boolean && !ForbiddenColumns.Contains(x.Key)).Select(x =>
                $"strpos(lower(coalesce({x.Value.Sql}, '')), lower({text})) > 0"));
        }
        var after = request.AfterId is { } afterId
            ? $"coalesce((SELECT rn FROM ranked WHERE \"Id\" = {query.Parameter(afterId)}), 0)" : "0";
        var sql = $"""
            WITH ranked AS (
                SELECT {Projection}, row_number() OVER (ORDER BY {query.Order}) AS rn
                FROM {Table} WHERE {query.Where}
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
        var security = SecurityPredicate;
        var sql = $"SELECT DISTINCT CASE WHEN {column.Sql} THEN 'Active' ELSE 'Inactive' END AS \"Value\" FROM {Table} WHERE {security} ORDER BY \"Value\"";
        return await db.Database.SqlQueryRaw<string>(sql).ToArrayAsync(ct);
    }
}
