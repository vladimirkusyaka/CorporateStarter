using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Application.MasterData.Countries.Commands
{
    public sealed record UpdateCountryCommand(
        Guid Id,
        UpdateCountryRequest Request)
        : IRequest<CommandResult<CountryDetailsDto>>;
}
