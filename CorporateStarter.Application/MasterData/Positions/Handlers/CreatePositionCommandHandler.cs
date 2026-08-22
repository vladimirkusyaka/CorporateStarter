using Mapster;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.MasterData.Positions.Commands;
using CorporateStarter.Application.MasterData.Positions.Models;
using CorporateStarter.Shared.Dtos.MasterData.Positions;


namespace CorporateStarter.Application.MasterData.Positions.Handlers
{
    public sealed class CreatePositionCommandHandler(
        IPositionReadRepository positionReadRepository,
        IPositionWriteRepository positionWriteRepository)
        : IRequestHandler<CreatePositionCommand, CommandResult<PositionDetailsDto>>
    {
        private readonly IPositionReadRepository _positionReadRepository = positionReadRepository;
        private readonly IPositionWriteRepository _positionWriteRepository = positionWriteRepository;

        public async Task<CommandResult<PositionDetailsDto>> Handle(
            CreatePositionCommand request,
            CancellationToken cancellationToken)
        {
            var values = request.Request.Adapt<PositionWriteValues>();
            values.IsActive = true;
            values.Normalize();

            if (string.IsNullOrWhiteSpace(values.Name))
                return CommandResult<PositionDetailsDto>.Failure(
                    "position.name_required",
                    "Position name is required.");

            if (await _positionReadRepository.ExistsByNameAsync(values.Name, null, cancellationToken))
                return CommandResult<PositionDetailsDto>.Failure(
                    "position.name_exists",
                    "Position name already exists.");

            var position = await _positionWriteRepository.AddAsync(values, cancellationToken);
            await _positionWriteRepository.SaveChangesAsync(cancellationToken);

            var dto = await _positionReadRepository.GetByIdAsync(position.Id, cancellationToken);

            return dto is null
                ? CommandResult<PositionDetailsDto>.Failure("not_found", "Position was not found.")
                : CommandResult<PositionDetailsDto>.Success(dto);
        }
    }
}
