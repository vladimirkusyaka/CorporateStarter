using MediatR;
using CorporateStarter.Application.MasterData.Positions.Queries;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;

namespace CorporateStarter.Application.MasterData.Positions.Handlers
{
    public sealed class GetPositionByIdQueryHandler
        : IRequestHandler<GetPositionByIdQuery, PositionDetailsDto?>
    {
        private readonly IPositionReadRepository _repository;

        public GetPositionByIdQueryHandler(IPositionReadRepository repository)
        {
            _repository = repository;
        }

        public Task<PositionDetailsDto?> Handle(
            GetPositionByIdQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
