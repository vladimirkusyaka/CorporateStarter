using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.MasterData.Countries.Queries;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Application.MasterData.Countries.Handlers
{
    public sealed class GetCountryByIdQueryHandler
        : IRequestHandler<GetCountryByIdQuery, CountryDetailsDto?>
    {
        private readonly ICountryReadRepository _repository;

        public GetCountryByIdQueryHandler(ICountryReadRepository repository)
        {
            _repository = repository;
        }

        public Task<CountryDetailsDto?> Handle(
            GetCountryByIdQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
