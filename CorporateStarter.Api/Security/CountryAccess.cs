using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
namespace CorporateStarter.Api.Security;

// Keep the existing Countries contract while using the shared permission rules.
public sealed class CountryAccess(IEntityAccess access) : ICountryAccess
{
    public CountryCapabilities Capabilities
    {
        get
        {
            var caps = access.For("countries");
            return new(caps.CanCreate, caps.CanUpdate, caps.CanDelete, caps.ViewInactive, caps.CanRestore);
        }
    }
}
