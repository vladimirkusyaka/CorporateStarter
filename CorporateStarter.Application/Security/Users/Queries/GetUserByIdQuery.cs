using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Shared.Dtos.Security.Users;

namespace CorporateStarter.Application.Security.Users.Queries
{
    public sealed record GetUserByIdQuery(Guid Id)
        : IRequest<UserDetailsDto?>;
}
