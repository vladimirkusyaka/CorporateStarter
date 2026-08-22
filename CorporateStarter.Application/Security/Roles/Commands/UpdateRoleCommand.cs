using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.Security.Roles;


namespace CorporateStarter.Application.Security.Roles.Commands
{
    public sealed record UpdateRoleCommand(
        Guid Id,
        UpdateRoleRequest Request)
        : IRequest<CommandResult<Unit>>;
}
