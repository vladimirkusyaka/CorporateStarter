using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Shared.Dtos.Permissions;

namespace CorporateStarter.Application.Security.Permissions.Queries
{
    public sealed record GetPermissionsQuery
        : IRequest<IReadOnlyList<PermissionListItemDto>>;
}
