using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
using Microsoft.EntityFrameworkCore;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Cities;

public sealed class CityTableRepository : SqlTableRepository<CityListItemDto>, ICityTableRepository
{
    private readonly AppDbContext _db;
    public CityTableRepository(AppDbContext db) : base(db) => _db = db;

    // A derived table exposes the same unqualified column names to query, ranked find and filters.
    protected override string Table => """
        (SELECT c."Id", c."Name", c."Region", c."CountryId", c."IsActive",
                country."Code" AS "CountryCode", country."Name" AS "CountryName"
         FROM "Cities" c INNER JOIN "Countries" country ON country."Id" = c."CountryId") AS city_rows
        """;
    protected override string Projection => "\"Id\", \"Name\", \"Region\", \"CountryId\", \"CountryCode\", \"CountryName\", \"IsActive\"";
    protected override string DefaultOrder => "lower(\"CountryName\") asc, lower(\"Name\") asc";
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } =
        new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = new("\"Name\""),
            ["region"] = new("\"Region\""),
            ["countryCode"] = new("\"CountryCode\""),
            ["countryName"] = new("\"CountryName\""),
            ["isActive"] = new("\"IsActive\"", true)
        };

    public async Task<LookupOption[]> SearchCountriesAsync(LookupRequest request, CancellationToken ct)
    {
        if (request.Limit is < 1 or > 50 || request.Text?.Length > 200)
            throw new ArgumentException("Invalid country lookup request.");
        var text = (request.Text ?? "").Trim().ToLowerInvariant();
        // Existing city writes accept any existing country, including inactive ones.
        return await _db.Countries.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(text) || x.Code.ToLower().Contains(text))
            .OrderBy(x => x.Name).ThenBy(x => x.Code).ThenBy(x => x.Id)
            .Take(request.Limit)
            .Select(x => new LookupOption(x.Id, x.Code + " - " + x.Name + (x.IsActive ? "" : " (Inactive)")))
            .ToArrayAsync(ct);
    }
}
