using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
namespace CorporateStarter.Api.Security;

public sealed class CountryAccess(IHttpContextAccessor context) : ICountryAccess
{
    private bool Has(string code) => context.HttpContext?.User.HasClaim("permission", code) == true;
    public CountryCapabilities Capabilities => new(Has(AppPermissions.CountriesCreate), Has(AppPermissions.CountriesUpdate),
        Has(AppPermissions.CountriesDelete), Has(AppPermissions.CountriesViewInactive), Has(AppPermissions.CountriesRestore));
}
