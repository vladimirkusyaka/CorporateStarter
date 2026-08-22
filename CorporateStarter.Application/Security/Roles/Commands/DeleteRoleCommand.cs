using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;


namespace CorporateStarter.Application.Security.Roles.Commands
{
    public sealed record DeleteRoleCommand(Guid Id)
        : IRequest<CommandResult<Unit>>;
}
