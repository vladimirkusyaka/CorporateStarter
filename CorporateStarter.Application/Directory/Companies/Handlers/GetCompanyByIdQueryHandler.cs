using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Application.Directory.Companies.Queries;
using CorporateStarter.Shared.Dtos.Directory.Companies;

namespace CorporateStarter.Application.Directory.Companies.Handlers
{
    public sealed class GetCompanyByIdQueryHandler
        : IRequestHandler<GetCompanyByIdQuery, CompanyDetailsDto?>
    {
        private readonly ICompanyReadRepository _repository;

        public GetCompanyByIdQueryHandler(ICompanyReadRepository repository)
        {
            _repository = repository;
        }

        public Task<CompanyDetailsDto?> Handle(
            GetCompanyByIdQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
