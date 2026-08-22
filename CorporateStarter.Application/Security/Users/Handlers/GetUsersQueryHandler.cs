using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Security.Users.Queries;
using CorporateStarter.Shared.Dtos.Security.Users;

namespace CorporateStarter.Application.Security.Users.Handlers
{
    public sealed class GetUsersQueryHandler(IUserReadRepository repository)
        : IRequestHandler<GetUsersQuery, IReadOnlyList<UserListItemDto>>
    {
        private readonly IUserReadRepository _repository = repository;

        public Task<IReadOnlyList<UserListItemDto>> Handle(
            GetUsersQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetListAsync(cancellationToken);
        }
    }
}