using Mapster;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.MasterData.Countries.Commands;
using CorporateStarter.Application.MasterData.Countries.Models;
using CorporateStarter.Shared.Dtos.MasterData.Countries;


namespace CorporateStarter.Application.MasterData.Countries.Handlers
{
    public sealed class CreateCountryCommandHandler
        : IRequestHandler<CreateCountryCommand, CommandResult<CountryDetailsDto>>
    {
        private readonly ICountryReadRepository _countryReadRepository;
        private readonly ICountryWriteRepository _countryWriteRepository;

        public CreateCountryCommandHandler(
            ICountryReadRepository countryReadRepository,
            ICountryWriteRepository countryWriteRepository)
        {
            _countryReadRepository = countryReadRepository;
            _countryWriteRepository = countryWriteRepository;
        }

        public async Task<CommandResult<CountryDetailsDto>> Handle(
            CreateCountryCommand request,
            CancellationToken cancellationToken)
        {
            var values = request.Request.Adapt<CountryWriteValues>();
            values.IsActive = true;
            values.Normalize();

            if (string.IsNullOrWhiteSpace(values.Code))
                return CommandResult<CountryDetailsDto>.Failure(
                    "country.code_required",
                    "Country code is required.");

            if (string.IsNullOrWhiteSpace(values.Name))
                return CommandResult<CountryDetailsDto>.Failure(
                    "country.name_required",
                    "Country name is required.");

            if (await _countryReadRepository.ExistsByCodeAsync(
                    values.Code,
                    null,
                    cancellationToken))
            {
                return CommandResult<CountryDetailsDto>.Failure(
                    "country.code_exists",
                    "Country code already exists.");
            }

            if (await _countryReadRepository.ExistsByNameAsync(
                    values.Name,
                    null,
                    cancellationToken))
            {
                return CommandResult<CountryDetailsDto>.Failure(
                    "country.name_exists",
                    "Country name already exists.");
            }

            var country = await _countryWriteRepository.AddAsync(
                values,
                cancellationToken);

            await _countryWriteRepository.SaveChangesAsync(cancellationToken);

            var dto = await _countryReadRepository.GetByIdAsync(
                country.Id,
                cancellationToken);

            return dto is null
                    ? CommandResult<CountryDetailsDto>.Failure("country.not_found", "Country was not found.")
                    : CommandResult<CountryDetailsDto>.Success(dto);
        }
    }
}
