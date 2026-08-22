using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Results;

namespace CorporateStarter.Application.Security.Users.Commands
{
    public sealed record DeleteUserCommand(Guid Id)
        : IRequest<CommandResult<Unit>>;
}
