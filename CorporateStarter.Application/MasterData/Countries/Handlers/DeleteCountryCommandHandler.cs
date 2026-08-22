using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Countries;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.MasterData.Countries.Commands;


namespace CorporateStarter.Application.MasterData.Countries.Handlers
{
    public sealed class DeleteCountryCommandHandler
       : IRequestHandler<DeleteCountryCommand, CommandResult<Unit>>
    {
        private readonly ICountryWriteRepository _countryWriteRepository;

        public DeleteCountryCommandHandler(ICountryWriteRepository countryWriteRepository)
        {
            _countryWriteRepository = countryWriteRepository;
        }

        public async Task<CommandResult<Unit>> Handle(
            DeleteCountryCommand request,
            CancellationToken cancellationToken)
        {
            var deactivated = await _countryWriteRepository.DeactivateAsync(
                request.Id,
                cancellationToken);

            if (!deactivated)
                return CommandResult<Unit>.Failure(
                    "country.not_found",
                    "Country was not found.");

            await _countryWriteRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
