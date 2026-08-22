using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Security.Roles.Commands;

namespace CorporateStarter.Application.Security.Roles.Handlers
{
    public sealed class DeleteRoleCommandHandler(
        IRoleReadRepository readRepository,
        IRoleWriteRepository writeRepository)
        : IRequestHandler<DeleteRoleCommand, CommandResult<Unit>>
    {
        private readonly IRoleReadRepository _readRepository = readRepository;
        private readonly IRoleWriteRepository _writeRepository = writeRepository;

        public async Task<CommandResult<Unit>> Handle(
            DeleteRoleCommand request,
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

            if (await _readRepository.IsAdministratorRoleAsync(
                request.Id,
                cancellationToken))
            {
                return CommandResult<Unit>.Failure(
                    "role.administrator_cannot_be_deleted",
                    "Administrator role cannot be deleted.");
            }

            await _writeRepository.DeactivateAsync(
                request.Id,
                cancellationToken);

            await _writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
