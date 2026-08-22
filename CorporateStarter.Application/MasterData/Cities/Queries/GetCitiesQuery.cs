using MediatR;
using CorporateStarter.Shared.Dtos.MasterData.Cities;

namespace CorporateStarter.Application.MasterData.Cities.Queries;

public sealed record GetCitiesQuery
    : IRequest<IReadOnlyList<CityListItemDto>>;

