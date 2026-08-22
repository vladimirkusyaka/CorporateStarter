using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Directory.Companies.Commands;


namespace CorporateStarter.Application.Directory.Companies.Handlers
{
    public sealed class DeleteCompanyCommandHandler
    : IRequestHandler<DeleteCompanyCommand, CommandResult<Unit>>
    {
        private readonly ICompanyWriteRepository _companyWriteRepository;

        public DeleteCompanyCommandHandler(ICompanyWriteRepository companyWriteRepository)
        {
            _companyWriteRepository = companyWriteRepository;
        }

        public async Task<CommandResult<Unit>> Handle(
            DeleteCompanyCommand request,
            CancellationToken cancellationToken)
        {
            var deactivated = await _companyWriteRepository.DeactivateAsync(
                request.Id,
                cancellationToken);

            if (!deactivated)
                return CommandResult<Unit>.Failure(
                    "company.not_found",
                    "Company was not found.");

            await _companyWriteRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
