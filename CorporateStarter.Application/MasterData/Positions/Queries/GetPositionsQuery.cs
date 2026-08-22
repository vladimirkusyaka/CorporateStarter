using MediatR;
using CorporateStarter.Shared.Dtos.MasterData.Positions;

namespace CorporateStarter.Application.MasterData.Positions.Queries;

public sealed record GetPositionsQuery
    : IRequest<IReadOnlyList<PositionListItemDto>>;