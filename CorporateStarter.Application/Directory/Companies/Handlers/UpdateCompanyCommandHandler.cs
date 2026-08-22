using Mapster;
using MediatR;
using CorporateStarter.Application.Directory.Companies.Models;
using CorporateStarter.Application.Directory.Companies.Commands;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Shared.Dtos.Directory.Companies;

namespace CorporateStarter.Application.Directory.Companies.Handlers
{
    public sealed class UpdateCompanyCommandHandler
    : IRequestHandler<UpdateCompanyCommand, CommandResult<CompanyDetailsDto>>
    {
        private readonly ICompanyReadRepository _companyReadRepository;
        private readonly ICompanyWriteRepository _companyWriteRepository;
        private readonly ICityReadRepository _cityReadRepository;

        public UpdateCompanyCommandHandler(
            ICompanyReadRepository companyReadRepository,
            ICompanyWriteRepository companyWriteRepository,
            ICityReadRepository cityReadRepository)
        {
            _companyReadRepository = companyReadRepository;
            _companyWriteRepository = companyWriteRepository;
            _cityReadRepository = cityReadRepository;
        }

        public async Task<CommandResult<CompanyDetailsDto>> Handle(
            UpdateCompanyCommand request,
            CancellationToken cancellationToken)
        {
            var values = request.Request.Adapt<CompanyWriteValues>();
            values.Id = request.Id;
            values.Normalize();

            if (string.IsNullOrWhiteSpace(values.Name))
                return CommandResult<CompanyDetailsDto>.Failure(
                    "company.name_required",
                    "Company name is required.");

            if (!string.IsNullOrWhiteSpace(values.Code)
                && await _companyReadRepository.ExistsByCodeAsync(values.Code, request.Id, cancellationToken))
            {
                return CommandResult<CompanyDetailsDto>.Failure(
                    "company.code_exists",
                    "Company code already exists.");
            }

            if (await _companyReadRepository.ExistsByNameAsync(values.Name, request.Id, cancellationToken))
            {
                return CommandResult<CompanyDetailsDto>.Failure(
                    "company.name_exists",
                    "Company name already exists.");
            }

            if (values.CityId is not null
                && !await _cityReadRepository.ExistsByIdAsync(values.CityId.Value, cancellationToken))
            {
                return CommandResult<CompanyDetailsDto>.Failure(
                    "company.city_not_found",
                    "City was not found.");
            }

            var updated = await _companyWriteRepository.UpdateAsync(values, cancellationToken);

            if (!updated)
                return CommandResult<CompanyDetailsDto>.Failure(
                    "company.not_found",
                    "Company was not found.");

            await _companyWriteRepository.SaveChangesAsync(cancellationToken);

            var dto = await _companyReadRepository.GetByIdAsync(request.Id, cancellationToken);

            return dto is null
                    ? CommandResult<CompanyDetailsDto>.Failure("company.not_found", "Company was not found.")
                    : CommandResult<CompanyDetailsDto>.Success(dto);
        }
    }
}
