using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using Microsoft.EntityFrameworkCore;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.Directory.Companies;

public sealed class CompanyTableRepository : SqlTableRepository<CompanyListItemDto>, ICompanyTableRepository
{
    private readonly AppDbContext _db;
    public CompanyTableRepository(AppDbContext db) : base(db) => _db = db;
    // LEFT JOIN preserves companies whose optional city has not been selected.
    protected override string Table => """
        (SELECT c."Id", c."Code", c."Name", c."LegalName", c."TaxNumber", c."VatId", c."Email", c."Phone", c."Website", c."Street", c."HouseNumber", c."PostalCode", c."Description", c."CityId", c."IsActive",
                ''::text AS "CityCode", coalesce(city."Name", '') AS "CityName",
                coalesce(country."Name", '') AS "CountryName"
         FROM "Companies" c LEFT JOIN "Cities" city ON city."Id" = c."CityId"
         LEFT JOIN "Countries" country ON country."Id" = city."CountryId") AS company_rows
        """;
    protected override string Projection => """
        "Id", "Code", "Name", "LegalName", "TaxNumber", "VatId", "Email", "Phone", "Website", "Street", "HouseNumber", "PostalCode", "Description", "CityId", "IsActive", "CityCode", "CityName", "CountryName"
        """;
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns { get; } =
        new Dictionary<string, SqlTableColumn>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = new("\"Name\""),
            ["code"] = new("\"Code\""),
            ["cityName"] = new("\"CityName\""),
            ["countryName"] = new("\"CountryName\""),
            ["email"] = new("\"Email\""),
            ["phone"] = new("\"Phone\""),
            ["isActive"] = new("\"IsActive\"", true)
        };
    public async Task<LookupOption[]> SearchCitiesAsync(LookupRequest request, CancellationToken ct)
    {
        if (request.Limit is < 1 or > 50 || request.Text?.Length > 200)
            throw new ArgumentException("Invalid city lookup request.");
        var text = (request.Text ?? "").Trim().ToLowerInvariant();
        // Company writes already accept existing inactive cities; show their status.
        return await _db.Cities.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(text) || (x.Region != null && x.Region.ToLower().Contains(text)) ||
                x.Country.Name.ToLower().Contains(text) || x.Country.Code.ToLower().Contains(text))
            .OrderBy(x => x.Country.Name).ThenBy(x => x.Name).ThenBy(x => x.Region).ThenBy(x => x.Id)
            .Take(request.Limit)
            .Select(x => new LookupOption(x.Id, x.Name + (x.Region == null ? "" : " / " + x.Region) +
                " — " + x.Country.Name + (x.IsActive ? "" : " (Inactive)")))
            .ToArrayAsync(ct);
    }
}
