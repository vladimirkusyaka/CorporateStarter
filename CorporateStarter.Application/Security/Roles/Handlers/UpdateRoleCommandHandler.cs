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
    public sealed class UpdateRoleCommandHandler(
        IRoleReadRepository readRepository,
        IRoleWriteRepository writeRepository)
        : IRequestHandler<UpdateRoleCommand, CommandResult<Unit>>
    {
        private readonly IRoleReadRepository _readRepository = readRepository;
        private readonly IRoleWriteRepository _writeRepository = writeRepository;

        public async Task<CommandResult<Unit>> Handle(
            UpdateRoleCommand request,
            CancellationToken cancellationToken)
        {
            if (!await _readRepository.ExistsByIdAsync(
                    request.Id,
                    cancellationToken))
            {
                return CommandResult<Unit>.Failure(
                    "role.not_found",
                    "Role not found.");
            }

            var values = request.Request.Adapt<RoleWriteValues>();
            values.Id = request.Id;
            values.Normalize();

            if (string.IsNullOrWhiteSpace(values.Name))
                return CommandResult<Unit>.Failure(
                    "role.name_required",
                    "Role name is required.");

            if (await _readRepository.ExistsByNameAsync(
                    values.Name,
                    excludeId: request.Id,
                    cancellationToken))
            {
                return CommandResult<Unit>.Failure(
                    "role.name_already_exists",
                    "Role with the same name already exists.");
            }

            if (!await _readRepository.AllPermissionsExistAsync(
                    values.PermissionIds,
                    cancellationToken))
            {
                return CommandResult<Unit>.Failure(
                    "role.permissions_not_found",
                    "One or more permissions do not exist.");
            }

            await _writeRepository.UpdateAsync(
                values,
                cancellationToken);

            await _writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
