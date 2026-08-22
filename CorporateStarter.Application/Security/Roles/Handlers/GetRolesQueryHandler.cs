using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Security.Roles.Queries;
using CorporateStarter.Shared.Dtos.Security.Roles;


namespace CorporateStarter.Application.Security.Roles.Handlers
{
    public sealed class GetRolesQueryHandler(IRoleReadRepository repository)
        : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleListItemDto>>
    {
        private readonly IRoleReadRepository _repository = repository;

        public Task<IReadOnlyList<RoleListItemDto>> Handle(
            GetRolesQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetListAsync(cancellationToken);
        }
    }
}