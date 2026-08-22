using MediatR;
using CorporateStarter.Application.MasterData.Cities.Queries;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;


namespace CorporateStarter.Application.MasterData.Cities.Handlers;

public class GetCitiesHandler : IRequestHandler<GetCitiesQuery, IReadOnlyList<CityListItemDto>>
{
    private readonly ICityReadRepository _repository;

    public GetCitiesHandler(ICityReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CityListItemDto>> Handle(
        GetCitiesQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetListAsync(cancellationToken);
    }
}
