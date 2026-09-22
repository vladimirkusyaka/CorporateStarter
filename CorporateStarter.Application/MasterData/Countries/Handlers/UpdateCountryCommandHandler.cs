using Mapster;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.MasterData.Countries.Commands;
using CorporateStarter.Application.MasterData.Countries.Models;
using CorporateStarter.Shared.Dtos.MasterData.Countries;


namespace CorporateStarter.Application.MasterData.Countries.Handlers
{
    public sealed class UpdateCountryCommandHandler
        : IRequestHandler<UpdateCountryCommand, CommandResult<CountryDetailsDto>>
    {
        private readonly ICountryReadRepository _countryReadRepository;
        private readonly ICountryWriteRepository _countryWriteRepository;

        public UpdateCountryCommandHandler(
            ICountryReadRepository countryReadRepository,
            ICountryWriteRepository countryWriteRepository)
        {
            _countryReadRepository = countryReadRepository;
            _countryWriteRepository = countryWriteRepository;
        }

        public async Task<CommandResult<CountryDetailsDto>> Handle(
            UpdateCountryCommand request,
            CancellationToken cancellationToken)
        {
            if (await _countryReadRepository.GetByIdAsync(request.Id, cancellationToken) is null)
                return CommandResult<CountryDetailsDto>.Failure("country.not_found", "Country was not found.");
            var values = request.Request.Adapt<CountryWriteValues>();
            values.Id = request.Id;
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
                    request.Id,
                    cancellationToken))
            {
                return CommandResult<CountryDetailsDto>.Failure(
                    "country.code_exists",
                    "Country code already exists.");
            }

            if (await _countryReadRepository.ExistsByNameAsync(
                    values.Name,
                    request.Id,
                    cancellationToken))
            {
                return CommandResult<CountryDetailsDto>.Failure(
                    "country.name_exists",
                    "Country name already exists.");
            }

            var updated = await _countryWriteRepository.UpdateAsync(
                values,
                cancellationToken);

            if (!updated)
                return CommandResult<CountryDetailsDto>.Failure(
                    "country.not_found",
                    "Country was not found.");

            await _countryWriteRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<CountryDetailsDto>.Success(new CountryDetailsDto
            {
                Id = request.Id,
                Code = values.Code,
                Name = values.Name,
                NativeName = values.NativeName,
                PhoneCode = values.PhoneCode,
                IsActive = values.IsActive!.Value
            });
        }
    }
}
