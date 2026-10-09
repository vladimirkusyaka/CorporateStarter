using CorporateStarter.Shared.Dtos.Directory.Persons;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using CorporateStarter.Shared.Dtos.Security.Users;
using CorporateStarter.Shared.Dtos.Security.Roles;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Shared.Dtos.Permissions;
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
[JsonSerializable(typeof(PagedResult<PermissionListItemDto>), TypeInfoPropertyName = "PermissionPage")]
[JsonSerializable(typeof(PagedResult<RoleListItemDto>), TypeInfoPropertyName = "RolePage")]
[JsonSerializable(typeof(RoleDetailsDto))]
[JsonSerializable(typeof(RoleCapabilities))]
[JsonSerializable(typeof(CreateRoleRequest))]
[JsonSerializable(typeof(UpdateRoleRequest))]
[JsonSerializable(typeof(ClientCommandResult))]
[JsonSerializable(typeof(PermissionListItemDto[]), TypeInfoPropertyName = "PermissionOptions")]
[JsonSerializable(typeof(UserDetailsDto))]
[JsonSerializable(typeof(UserCapabilities))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(ChangeUserPasswordRequest))]
[JsonSerializable(typeof(PagedResult<UserTableItemDto>), TypeInfoPropertyName = "UserPage")]
[JsonSerializable(typeof(RoleListItemDto[]), TypeInfoPropertyName = "RoleOptions")]
[JsonSerializable(typeof(CompanyListItemDto[]), TypeInfoPropertyName = "Companies")]
[JsonSerializable(typeof(PagedResult<CompanyListItemDto>), TypeInfoPropertyName = "CompanyPage")]
[JsonSerializable(typeof(CompanyDetailsDto))]
[JsonSerializable(typeof(CreateCompanyRequest))]
[JsonSerializable(typeof(UpdateCompanyRequest))]
[JsonSerializable(typeof(PersonListItemDto[]), TypeInfoPropertyName = "Persons")]
[JsonSerializable(typeof(PagedResult<PersonListItemDto>), TypeInfoPropertyName = "PersonPage")]
[JsonSerializable(typeof(PersonDetailsDto))]
[JsonSerializable(typeof(CreatePersonRequest))]
[JsonSerializable(typeof(UpdatePersonRequest))]
internal partial class ClientJsonContext : JsonSerializerContext
{
}