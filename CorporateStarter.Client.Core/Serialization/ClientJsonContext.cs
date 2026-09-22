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
internal partial class ClientJsonContext : JsonSerializerContext
{
}