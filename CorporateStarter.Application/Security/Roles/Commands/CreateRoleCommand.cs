using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.Security.Roles;

namespace CorporateStarter.Application.Security.Roles.Commands
{
    public sealed record CreateRoleCommand(CreateRoleRequest Request)
        : IRequest<CommandResult<Guid>>;
}
