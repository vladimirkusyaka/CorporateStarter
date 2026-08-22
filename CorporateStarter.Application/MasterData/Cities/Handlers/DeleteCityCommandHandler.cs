using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.MasterData.Cities.Commands;

namespace CorporateStarter.Application.MasterData.Cities.Handlers
{
    public sealed class DeleteCityCommandHandler
        : IRequestHandler<DeleteCityCommand, CommandResult<Unit>>
    {
        private readonly ICityWriteRepository _cityWriteRepository;

        public DeleteCityCommandHandler(ICityWriteRepository cityWriteRepository)
        {
            _cityWriteRepository = cityWriteRepository;
        }

        public async Task<CommandResult<Unit>> Handle(
            DeleteCityCommand request,
            CancellationToken cancellationToken)
        {
            var deactivated = await _cityWriteRepository.DeactivateAsync(
                request.Id,
                cancellationToken);

            if (!deactivated)
            {
                return CommandResult<Unit>.Failure(
                    "city.not_found",
                    "City was not found.");
            }

            await _cityWriteRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
