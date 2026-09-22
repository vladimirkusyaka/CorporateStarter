using CorporateStarter.Shared.Dtos.MasterData.Countries;
namespace CorporateStarter.Application.Common.Security;

public interface ICountryAccess { CountryCapabilities Capabilities { get; } }
