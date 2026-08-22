using MediatR;
using CorporateStarter.Application.Common.Results;

namespace CorporateStarter.Application.Directory.Companies.Commands
{
    public sealed record DeleteCompanyCommand(Guid Id) : IRequest<CommandResult<Unit>>;
}
