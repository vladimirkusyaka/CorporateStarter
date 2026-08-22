using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Application.MasterData.Cities.Queries;
using CorporateStarter.Shared.Dtos.MasterData.Cities;


namespace CorporateStarter.Application.MasterData.Cities.Handlers
{
    public sealed class GetCityByIdQueryHandler
        : IRequestHandler<GetCityByIdQuery, CityDetailsDto?>
    {
        private readonly ICityReadRepository _repository;

        public GetCityByIdQueryHandler(ICityReadRepository repository)
        {
            _repository = repository;
        }

        public Task<CityDetailsDto?> Handle(
            GetCityByIdQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
