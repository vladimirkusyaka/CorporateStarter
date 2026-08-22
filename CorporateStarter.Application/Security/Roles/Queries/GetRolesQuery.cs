using MediatR;
using CorporateStarter.Shared.Dtos.Security.Roles;

namespace CorporateStarter.Application.Security.Roles.Queries
{
    public sealed record GetRolesQuery
            : IRequest<IReadOnlyList<RoleListItemDto>>;
}
