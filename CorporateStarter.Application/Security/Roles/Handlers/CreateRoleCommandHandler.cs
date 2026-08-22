using System;
using System.Text;
using System.Collections.Generic;
using Mapster;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Security.Roles.Commands;
using CorporateStarter.Application.Security.Roles.Models;

namespace CorporateStarter.Application.Security.Roles.Handlers
{
    public sealed class CreateRoleCommandHandler(
        IRoleReadRepository readRepository,
        IRoleWriteRepository writeRepository)
        : IRequestHandler<CreateRoleCommand, CommandResult<Guid>>
    {
        private readonly IRoleReadRepository _readRepository = readRepository;
        private readonly IRoleWriteRepository _writeRepository = writeRepository;

        public async Task<CommandResult<Guid>> Handle(
            CreateRoleCommand request,
            CancellationToken cancellationToken)
        {
            var values = request.Request.Adapt<RoleWriteValues>();
            values.Normalize();

            if (string.IsNullOrWhiteSpace(values.Name))
                return CommandResult<Guid>.Failure(
                    "role.name_required",
                    "Role name is required.");

            if (await _readRepository.ExistsByNameAsync(
                    values.Name,
                    excludeId: null,
                    cancellationToken))
            {
                return CommandResult<Guid>.Failure(
                    "role.name_already_exists",
                    "Role with the same name already exists.");
            }

            if (!await _readRepository.AllPermissionsExistAsync(
                    values.PermissionIds,
                    cancellationToken))
            {
                return CommandResult<Guid>.Failure(
                    "role.permissions_not_found",
                    "One or more permissions do not exist.");
            }

            var id = await _writeRepository.AddAsync(
                values,
                cancellationToken);

            await _writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Guid>.Success(id);
        }
    }
}
