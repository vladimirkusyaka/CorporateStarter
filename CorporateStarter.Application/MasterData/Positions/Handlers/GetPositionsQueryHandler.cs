using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Application.MasterData.Positions.Queries;
using CorporateStarter.Shared.Dtos.MasterData.Positions;

namespace CorporateStarter.Application.MasterData.Positions.Handlers;

public sealed class GetPositionsQueryHandler
    : IRequestHandler<GetPositionsQuery, IReadOnlyList<PositionListItemDto>>
{
    private readonly IPositionReadRepository _repository;

    public GetPositionsQueryHandler(IPositionReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<PositionListItemDto>> Handle(
        GetPositionsQuery request,
        CancellationToken cancellationToken)
    {
        return await _repository.GetListAsync(cancellationToken);
    }
}