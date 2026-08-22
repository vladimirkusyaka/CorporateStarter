using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.MasterData.Positions;


namespace CorporateStarter.Application.MasterData.Positions.Commands
{
    public sealed record UpdatePositionCommand(
        Guid Id,
        UpdatePositionRequest Request)
        : IRequest<CommandResult<PositionDetailsDto>>;
}
