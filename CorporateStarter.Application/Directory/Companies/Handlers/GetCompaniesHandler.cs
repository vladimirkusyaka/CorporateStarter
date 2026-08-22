using MediatR;
using CorporateStarter.Application.Directory.Companies.Queries;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;

namespace CorporateStarter.Application.Directory.Companies.Handlers
{
    public class GetCompaniesHandler : IRequestHandler<GetCompaniesQuery, IReadOnlyList<CompanyListItemDto>>
    {
        private readonly ICompanyReadRepository _repository;

        public GetCompaniesHandler(ICompanyReadRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<CompanyListItemDto>> Handle(
            GetCompaniesQuery request,
            CancellationToken cancellationToken)
        {
            return await _repository.GetListAsync(cancellationToken);
        }
    }
}