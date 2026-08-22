using MediatR;
using CorporateStarter.Shared.Dtos.Security.Users;

namespace CorporateStarter.Application.Security.Users.Queries
{
    public sealed record GetUsersQuery
        : IRequest<IReadOnlyList<UserListItemDto>>;
}
