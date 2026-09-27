using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Persistence.Tables;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.MasterData.Countries;

public sealed class CountryTableRepository(AppDbContext db, ICountryAccess access)
    : SqlTableRepository<CountryListItemDto>(db), ICountryTableRepository
{
    protected override string Table => "\"Countries\"";
    protected override string Projection => "\"Id\", \"Code\", \"Name\", \"NativeName\", \"PhoneCode\", \"IsActive\"";
    private static readonly Dictionary<string, SqlTableColumn> ColumnMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["code"] = new("\"Code\""),
        ["name"] = new("\"Name\""),
        ["nativeName"] = new("\"NativeName\""),
        ["phoneCode"] = new("\"PhoneCode\""),
        ["isActive"] = new("\"IsActive\"", true)
    };
    protected override IReadOnlyDictionary<string, SqlTableColumn> Columns => ColumnMap;
    protected override string SecurityPredicate => access.Capabilities.ViewInactive ? "TRUE" : "\"IsActive\" = TRUE";
    protected override ISet<string> ForbiddenColumns => new HashSet<string>(
        access.Capabilities.ViewInactive ? [] : ["isActive"], StringComparer.OrdinalIgnoreCase);
}
