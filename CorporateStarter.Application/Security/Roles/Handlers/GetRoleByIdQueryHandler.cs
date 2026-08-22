using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Security.Roles.Queries;
using CorporateStarter.Shared.Dtos.Security.Roles;

namespace CorporateStarter.Application.Security.Roles.Handlers
{
    public sealed class GetRoleByIdQueryHandler(IRoleReadRepository repository)
        : IRequestHandler<GetRoleByIdQuery, RoleDetailsDto?>
    {
        private readonly IRoleReadRepository _repository = repository;

        public Task<RoleDetailsDto?> Handle(
            GetRoleByIdQuery request,
            CancellationToken cancellationToken)
        {
            return _repository.GetByIdAsync(
                request.Id,
                cancellationToken);
        }
    }
}
