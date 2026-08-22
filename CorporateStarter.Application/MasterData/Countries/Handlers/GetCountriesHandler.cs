using MediatR;
using CorporateStarter.Application.MasterData.Countries.Queries;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Application.MasterData.Countries.Handlers;

public class GetCountriesHandler
    : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryListItemDto>>
{
    private readonly ICountryReadRepository _repository;

    public GetCountriesHandler(ICountryReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CountryListItemDto>> Handle(GetCountriesQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetListAsync(cancellationToken);
    }
}
