using Mapster;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.MasterData.Positions.Commands;
using CorporateStarter.Application.MasterData.Positions.Models;
using CorporateStarter.Shared.Dtos.MasterData.Positions;


namespace CorporateStarter.Application.MasterData.Positions.Handlers
{
    public sealed class UpdatePositionCommandHandler(
        IPositionReadRepository positionReadRepository,
        IPositionWriteRepository positionWriteRepository)
        : IRequestHandler<UpdatePositionCommand, CommandResult<PositionDetailsDto>>
    {
        private readonly IPositionReadRepository _positionReadRepository = positionReadRepository;
        private readonly IPositionWriteRepository _positionWriteRepository = positionWriteRepository;

        public async Task<CommandResult<PositionDetailsDto>> Handle(
            UpdatePositionCommand request,
            CancellationToken cancellationToken)
        {
            var values = request.Request.Adapt<PositionWriteValues>();
            values.Id = request.Id;
            values.Normalize();

            if (string.IsNullOrWhiteSpace(values.Name))
                return CommandResult<PositionDetailsDto>.Failure(
                    "position.name_required",
                    "Position name is required.");

            if (await _positionReadRepository.ExistsByNameAsync(values.Name, request.Id, cancellationToken))
                return CommandResult<PositionDetailsDto>.Failure(
                    "position.name_exists",
                    "Position name already exists.");

            var updated = await _positionWriteRepository.UpdateAsync(values, cancellationToken);

            if (!updated)
                return CommandResult<PositionDetailsDto>.Failure(
                    "position.not_found",
                    "Position was not found.");

            await _positionWriteRepository.SaveChangesAsync(cancellationToken);

            var dto = await _positionReadRepository.GetByIdAsync(request.Id, cancellationToken);

            return dto is null
                ? CommandResult<PositionDetailsDto>.Failure("not_found", "Position was not found.")
                : CommandResult<PositionDetailsDto>.Success(dto);
        }
    }
}
