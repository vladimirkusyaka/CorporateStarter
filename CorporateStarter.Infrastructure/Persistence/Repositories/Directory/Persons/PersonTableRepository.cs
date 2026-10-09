using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Persons;
using Microsoft.EntityFrameworkCore;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Persons;

public sealed class PersonTableRepository : SqlTableRepository<PersonListItemDto>, IPersonTableRepository
{
    private readonly AppDbContext _db;
    public PersonTableRepository(AppDbContext db) : base(db) => _db = db;
    // Persons is the API name; the existing database table is People.
    // Optional relationships must not remove people from the result.
    protected override string Table => """
        (SELECT p."Id", p."Code", p."FirstName", p."MiddleName", p."LastName", p."Email", p."Phone", p."Description", p."DateOfBirth", p."CompanyId", p."PositionId", p."IsActive",
                company."Code" AS "CompanyCode", coalesce(company."Name", '') AS "CompanyName",
                coalesce(position."Name", '') AS "PositionName"
         FROM "People" p LEFT JOIN "Companies" company ON company."Id" = p."CompanyId"
         LEFT JOIN "Positions" position ON position."Id" = p."PositionId") AS person_rows
        """;
    protected override string Projection => """
        "Id", "Code", "FirstName", "MiddleName", "LastName", "Email", "Phone", "Description", "DateOfBirth", "CompanyId", "PositionId", "IsActive", "CompanyCode", "CompanyName", "PositionName"
        """;
    protected override string DefaultOrder => "lower(\"FirstName\") asc, lower(coalesce(\"LastName\", '')) asc";
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } =
        new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
        {
            ["firstName"] = new("\"FirstName\""),
            ["lastName"] = new("\"LastName\""),
            ["code"] = new("\"Code\""),
            ["companyName"] = new("\"CompanyName\""),
            ["positionName"] = new("\"PositionName\""),
            ["email"] = new("\"Email\""),
            ["phone"] = new("\"Phone\""),
            ["isActive"] = new("\"IsActive\"", true)
        };
    private static string LookupText(LookupRequest request)
    {
        if (request.Limit is < 1 or > 50 || request.Text?.Length > 200)
            throw new ArgumentException("Invalid lookup request.");
        return (request.Text ?? "").Trim().ToLowerInvariant();
    }
    public async Task<LookupOption[]> SearchCompaniesAsync(LookupRequest request, CancellationToken ct)
    {
        var text = LookupText(request);
        // Preserve existing API behavior: inactive references may be assigned too.
        return await _db.Companies.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(text) || (x.Code != null && x.Code.ToLower().Contains(text)))
            .OrderBy(x => x.Name).ThenBy(x => x.Id).Take(request.Limit)
            .Select(x => new LookupOption(x.Id, (x.Code == null ? "" : x.Code + " — ") + x.Name + (x.IsActive ? "" : " (Inactive)")))
            .ToArrayAsync(ct);
    }
    public async Task<LookupOption[]> SearchPositionsAsync(LookupRequest request, CancellationToken ct)
    {
        var text = LookupText(request);
        return await _db.Positions.AsNoTracking().Where(x => x.Name.ToLower().Contains(text))
            .OrderBy(x => x.Name).ThenBy(x => x.Id).Take(request.Limit)
            .Select(x => new LookupOption(x.Id, x.Name + (x.IsActive ? "" : " (Inactive)")))
            .ToArrayAsync(ct);
    }
}
