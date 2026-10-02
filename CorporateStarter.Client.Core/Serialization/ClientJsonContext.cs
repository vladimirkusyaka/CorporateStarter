using CorporateStarter.Shared.Dtos.MasterData.Cities;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using System.Text.Json.Serialization;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Client.Core.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CountryListItemDto[]), TypeInfoPropertyName = "Countries")]
[JsonSerializable(typeof(CreateCountryRequest))]
[JsonSerializable(typeof(UpdateCountryRequest))]
[JsonSerializable(typeof(CountryDetailsDto))]
[JsonSerializable(typeof(CountryCapabilities))]
[JsonSerializable(typeof(TableRequest))]
[JsonSerializable(typeof(TableFindRequest))]
[JsonSerializable(typeof(TableFindResult))]
[JsonSerializable(typeof(TableValuesRequest))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(PagedResult<CountryListItemDto>), TypeInfoPropertyName = "CountryPage")]
[JsonSerializable(typeof(PositionListItemDto[]), TypeInfoPropertyName = "Positions")]
[JsonSerializable(typeof(CreatePositionRequest))]
[JsonSerializable(typeof(UpdatePositionRequest))]
[JsonSerializable(typeof(PositionDetailsDto))]
[JsonSerializable(typeof(TableCapabilities))]
[JsonSerializable(typeof(PagedResult<PositionListItemDto>), TypeInfoPropertyName = "PositionPage")]
[JsonSerializable(typeof(CityListItemDto[]), TypeInfoPropertyName = "Cities")]
[JsonSerializable(typeof(CreateCityRequest))]
[JsonSerializable(typeof(UpdateCityRequest))]
[JsonSerializable(typeof(CityDetailsDto))]
[JsonSerializable(typeof(PagedResult<CityListItemDto>), TypeInfoPropertyName = "CityPage")]
[JsonSerializable(typeof(LookupRequest))]
[JsonSerializable(typeof(LookupOption[]), TypeInfoPropertyName = "LookupOptions")]
internal partial class ClientJsonContext : JsonSerializerContext
{
}