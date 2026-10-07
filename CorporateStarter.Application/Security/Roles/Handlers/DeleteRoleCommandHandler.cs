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
            var current = await _readRepository.GetByIdAsync(request.Id, cancellationToken);
            if (current is null)
                return CommandResult<Unit>.Failure("role.not_found", "Role not found.");
            if (current.IsSystemRole || current.Name == "Administrator")
                return CommandResult<Unit>.Failure("role.system_role_read_only", "System roles are read-only.");

            await _writeRepository.DeactivateAsync(
                request.Id,
                cancellationToken);

            await _writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
