using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Security.Permissions.Queries;
using CorporateStarter.Shared.Dtos.Permissions;

namespace CorporateStarter.Application.Security.Permissions.Handlers
{
    public sealed class GetPermissionsQueryHandler(IPermissionReadRepository repository)
                        : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionListItemDto>>
    {
        private readonly IPermissionReadRepository _repository = repository;

        public Task<IReadOnlyList<PermissionListItemDto>> Handle(
            GetPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetListAsync(cancellationToken);
        }
    }
}
