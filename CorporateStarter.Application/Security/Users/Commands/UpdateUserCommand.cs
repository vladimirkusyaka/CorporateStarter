using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Shared.Dtos.Security.Users;

namespace CorporateStarter.Application.Security.Users.Commands
{
    public sealed record UpdateUserCommand(
        Guid Id,
        UpdateUserRequest Request)
        : IRequest<CommandResult<Unit>>;
}
