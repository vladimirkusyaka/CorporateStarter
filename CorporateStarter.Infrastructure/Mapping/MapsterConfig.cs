using Mapster;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Core.Entities.MasterData;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
using CorporateStarter.Shared.Dtos.Directory.Persons;
using CorporateStarter.Shared.Dtos.Security.Users;

namespace CorporateStarter.Infrastructure.Mapping;

public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<City, CityListItemDto>
            .NewConfig()
            .Map(dest => dest.CountryCode, src => src.Country.Code)
            .Map(dest => dest.CountryName, src => src.Country.NativeName ?? src.Country.Name);

        TypeAdapterConfig<City, CityDetailsDto>
            .NewConfig()
            .Map(dest => dest.CountryCode, src => src.Country.Code)
            .Map(dest => dest.CountryName, src => src.Country.NativeName ?? src.Country.Name);

        TypeAdapterConfig<Company, CompanyListItemDto>
            .NewConfig()
            .Map(dest => dest.CityName, src => src.City != null ? src.City.Name : null)
            .Map(dest => dest.CountryName,
                src => src.City != null
                    ? src.City.Country.NativeName ?? src.City.Country.Name
                    : null);

        TypeAdapterConfig<Person, PersonListItemDto>
            .NewConfig()
            .Map(dest => dest.CompanyName, src => src.Company != null ? src.Company.Name : null)
            .Map(dest => dest.PositionName, src => src.Position != null ? src.Position.Name : null);

        TypeAdapterConfig<Person, PersonDetailsDto>
            .NewConfig()
            .Map(dest => dest.CompanyCode, src => src.Company != null ? src.Company.Code : null)
            .Map(dest => dest.CompanyName, src => src.Company != null ? src.Company.Name : null)
            .Map(dest => dest.PositionName, src => src.Position != null ? src.Position.Name : null);

        TypeAdapterConfig<User, UserListItemDto>
            .NewConfig()
            .Map(dest => dest.PersonName,
            src => src.Person != null
                    ? src.Person.FirstName
                    + " "
                    + (src.Person.MiddleName ?? "")
                    + " "
                    + (src.Person.LastName ?? "")
            : null);

        TypeAdapterConfig<Company, CompanyDetailsDto>
            .NewConfig()
            .Map(
            destination => destination.CityName,
                    source => source.City != null ? source.City.Name : null)
            .Map(
                    destination => destination.CountryName,
                    source => source.City != null
                    ? source.City.Country.NativeName ?? source.City.Country.Name
            : null);
    }
}
