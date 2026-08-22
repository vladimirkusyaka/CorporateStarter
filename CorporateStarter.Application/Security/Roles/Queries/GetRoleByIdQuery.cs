using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Shared.Dtos.Security.Roles;

namespace CorporateStarter.Application.Security.Roles.Queries
{
    public sealed record GetRoleByIdQuery(Guid Id)
        : IRequest<RoleDetailsDto?>;
}
