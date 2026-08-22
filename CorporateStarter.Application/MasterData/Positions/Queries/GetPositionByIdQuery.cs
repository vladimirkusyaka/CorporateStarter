using MediatR;
using CorporateStarter.Shared.Dtos.MasterData.Positions;

namespace CorporateStarter.Application.MasterData.Positions.Queries
{
    public sealed record GetPositionByIdQuery(Guid Id)
        : IRequest<PositionDetailsDto?>;
}
