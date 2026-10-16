using MediatR;
using Mapster;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Directory.Persons.Commands;
using CorporateStarter.Application.Directory.Persons.Models;
using CorporateStarter.Shared.Dtos.Directory.Persons;

namespace CorporateStarter.Application.Directory.Persons.Handlers
{
    public sealed class UpdatePersonCommandHandler
            : IRequestHandler<UpdatePersonCommand, CommandResult<PersonDetailsDto>>
    {
        private readonly IPersonReadRepository _personReadRepository;
        private readonly IPersonWriteRepository _personWriteRepository;
        private readonly ICompanyReadRepository _companyReadRepository;
        private readonly IPositionReadRepository _positionReadRepository;

        public UpdatePersonCommandHandler(
            IPersonReadRepository personReadRepository,
            IPersonWriteRepository personWriteRepository,
            ICompanyReadRepository companyReadRepository,
            IPositionReadRepository positionReadRepository)
        {
            _personReadRepository = personReadRepository;
            _personWriteRepository = personWriteRepository;
            _companyReadRepository = companyReadRepository;
            _positionReadRepository = positionReadRepository;
        }

        public async Task<CommandResult<PersonDetailsDto>> Handle(
            UpdatePersonCommand request,
            CancellationToken cancellationToken)
        {
            var current = await _personReadRepository.GetByIdAsync(request.Id, cancellationToken);
            if (current is null)
                return CommandResult<PersonDetailsDto>.Failure("person.not_found", "Person was not found.");

            var values = request.Request.Adapt<PersonWriteValues>();
            values.Id = request.Id;
            values.Normalize();

            var validationError = await ValidateAsync(values, current, cancellationToken);

            if (validationError is not null)
                return validationError;

            var updated = await _personWriteRepository.UpdateAsync(
                values,
                cancellationToken);

            if (!updated)
            {
                return CommandResult<PersonDetailsDto>.Failure(
                    "person.not_found",
                    "Person was not found.");
            }

            await _personWriteRepository.SaveChangesAsync(cancellationToken);

            var dto = await _personReadRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

            return dto is null
                ? CommandResult<PersonDetailsDto>.Failure("person.not_found", "Person was not found.")
                : CommandResult<PersonDetailsDto>.Success(dto);
        }

        private async Task<CommandResult<PersonDetailsDto>?> ValidateAsync(
            PersonWriteValues values,
            PersonDetailsDto current,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(values.FirstName))
            {
                return CommandResult<PersonDetailsDto>.Failure(
                    "person.first_name_required",
                    "Person first name is required.");
            }

            if (values.CompanyId != current.CompanyId && values.CompanyId is not null
                && !await _companyReadRepository.ExistsByIdAsync(values.CompanyId.Value, cancellationToken))
            {
                return CommandResult<PersonDetailsDto>.Failure(
                    "person.company_not_found",
                    "Company was not found.");
            }

            if (values.PositionId != current.PositionId && values.PositionId is not null
                && !await _positionReadRepository.ExistsByIdAsync(values.PositionId.Value, cancellationToken))
            {
                return CommandResult<PersonDetailsDto>.Failure(
                    "person.position_not_found",
                    "Position was not found.");
            }

            return null;
        }
    }
}
