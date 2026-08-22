using System;
using System.Text;
using System.Collections.Generic;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Security.Users.Commands;


namespace CorporateStarter.Application.Security.Users.Handlers
{
    public sealed class DeleteUserCommandHandler(
            IUserReadRepository readRepository,
            IUserWriteRepository writeRepository,
            ICurrentUserService currentUserService)
            : IRequestHandler<DeleteUserCommand, CommandResult<Unit>>
    {
        public async Task<CommandResult<Unit>> Handle(
            DeleteUserCommand request,
            CancellationToken cancellationToken)
        {
            if (!await readRepository.ExistsByIdAsync(request.Id, cancellationToken))
                return CommandResult<Unit>.Failure("user.not_found", "User not found.");

            if (currentUserService.UserId == request.Id)
            {
                return CommandResult<Unit>.Failure(
                    "user.self_deactivation_not_allowed",
                    "You cannot deactivate your own user account.");
            }

            var isActiveAdministrator = await readRepository.IsActiveAdministratorAsync(
                request.Id,
                cancellationToken);

            if (isActiveAdministrator &&
                !await readRepository.HasOtherActiveAdministratorAsync(
                    request.Id,
                    cancellationToken))
            {
                return CommandResult<Unit>.Failure(
                    "user.last_administrator_cannot_be_deactivated",
                    "Last active administrator cannot be deactivated.");
            }

            await writeRepository.DeactivateAsync(request.Id, cancellationToken);
            await writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
