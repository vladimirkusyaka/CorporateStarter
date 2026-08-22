using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.MasterData.Positions.Commands;

namespace CorporateStarter.Application.MasterData.Positions.Handlers
{
    public sealed class DeletePositionCommandHandler
    : IRequestHandler<DeletePositionCommand, CommandResult<Unit>>
    {
        private readonly IPositionWriteRepository _positionWriteRepository;

        public DeletePositionCommandHandler(IPositionWriteRepository positionWriteRepository)
        {
            _positionWriteRepository = positionWriteRepository;
        }

        public async Task<CommandResult<Unit>> Handle(
            DeletePositionCommand request,
            CancellationToken cancellationToken)
        {
            var deactivated = await _positionWriteRepository.DeactivateAsync(
                request.Id,
                cancellationToken);

            if (!deactivated)
                return CommandResult<Unit>.Failure(
                    "position.not_found",
                    "Position was not found.");

            await _positionWriteRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
