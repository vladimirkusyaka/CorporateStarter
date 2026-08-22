using Mapster;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.MasterData.Cities.Commands;
using CorporateStarter.Application.MasterData.Cities.Models;
using CorporateStarter.Shared.Dtos.MasterData.Cities;

namespace CorporateStarter.Application.MasterData.Cities.Handlers
{
    public sealed class CreateCityCommandHandler
        : IRequestHandler<CreateCityCommand, CommandResult<CityDetailsDto>>
    {
        private readonly ICityReadRepository _cityReadRepository;
        private readonly ICityWriteRepository _cityWriteRepository;
        private readonly ICountryReadRepository _countryReadRepository;

        public CreateCityCommandHandler(
            ICityReadRepository cityReadRepository,
            ICityWriteRepository cityWriteRepository,
            ICountryReadRepository countryReadRepository)
        {
            _cityReadRepository = cityReadRepository;
            _cityWriteRepository = cityWriteRepository;
            _countryReadRepository = countryReadRepository;
        }

        public async Task<CommandResult<CityDetailsDto>> Handle(
            CreateCityCommand request,
            CancellationToken cancellationToken)
        {
            var values = request.Request.Adapt<CityWriteValues>();
            values.IsActive = true;
            values.Normalize();

            var validationError = await ValidateAsync(
                values,
                excludeId: null,
                cancellationToken);

            if (validationError is not null)
                return validationError;

            var city = await _cityWriteRepository.AddAsync(
                values,
                cancellationToken);

            await _cityWriteRepository.SaveChangesAsync(cancellationToken);

            var dto = await _cityReadRepository.GetByIdAsync(
                city.Id,
                cancellationToken);

            return dto is null
                ? CommandResult<CityDetailsDto>.Failure("not_found", "City was not found.")
                : CommandResult<CityDetailsDto>.Success(dto);
        }

        private async Task<CommandResult<CityDetailsDto>?> ValidateAsync(
            CityWriteValues values,
            Guid? excludeId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(values.Name))
            {
                return CommandResult<CityDetailsDto>.Failure(
                    "city.name_required",
                    "City name is required.");
            }

            if (values.CountryId == Guid.Empty)
            {
                return CommandResult<CityDetailsDto>.Failure(
                    "city.country_required",
                    "Country is required.");
            }

            if (!await _countryReadRepository.ExistsByIdAsync(
                    values.CountryId,
                    cancellationToken))
            {
                return CommandResult<CityDetailsDto>.Failure(
                    "city.country_not_found",
                    "Country was not found.");
            }

            if (await _cityReadRepository.ExistsByNameAsync(
                    values.CountryId,
                    values.Name,
                    values.Region,
                    excludeId,
                    cancellationToken))
            {
                return CommandResult<CityDetailsDto>.Failure(
                    "city.name_exists",
                    "City with the same name and region already exists in this country.");
            }

            return null;
        }
    }
}
