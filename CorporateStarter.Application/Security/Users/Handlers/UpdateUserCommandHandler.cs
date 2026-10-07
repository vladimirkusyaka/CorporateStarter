using System;
using System.Text;
using System.Collections.Generic;
using Mapster;
using MediatR;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Security.Users.Commands;
using CorporateStarter.Application.Security.Users.Models;
using CorporateStarter.Application.Security.Users.Validation;


namespace CorporateStarter.Application.Security.Users.Handlers
{
    public sealed class UpdateUserCommandHandler(
        IUserReadRepository readRepository,
        IUserWriteRepository writeRepository,
        ICurrentUserService currentUserService,
        IRoleReadRepository roleRepository)
        : IRequestHandler<UpdateUserCommand, CommandResult<Unit>>
    {
        public async Task<CommandResult<Unit>> Handle(
            UpdateUserCommand request,
            CancellationToken cancellationToken)
        {
            var current = await readRepository.GetByIdAsync(request.Id, cancellationToken);
            if (current is null)
                return CommandResult<Unit>.Failure("user.not_found", "User not found.");

            var values = request.Request.Adapt<UserWriteValues>();
            values.Id = request.Id;
            values.ReplaceRoles = request.Request.RoleIds is not null;
            values.RoleIds = request.Request.RoleIds ?? current.Roles.Select(x => x.RoleId).ToArray();
            values.Normalize();

            if (!values.IsActive && currentUserService.UserId == request.Id)
                return CommandResult<Unit>.Failure("user.self_deactivation_not_allowed", "You cannot deactivate your own user account.");

            var roles = await roleRepository.GetListAsync(cancellationToken);
            var activeIds = roles.Where(x => x.IsActive).Select(x => x.Id).ToHashSet();
            var previousIds = current.Roles.Select(x => x.RoleId).ToHashSet();
            if (values.RoleIds.Any(id => !activeIds.Contains(id) && !previousIds.Contains(id)))
                return CommandResult<Unit>.Failure("user.roles_not_found", "Select existing active roles.");
            if (values.IsActive && !values.RoleIds.Any(activeIds.Contains))
                return CommandResult<Unit>.Failure("user.role_required", "Active user must have at least one active role.");

            var retainsAdmin = values.IsActive && roles.Any(x => x.IsActive && x.Name == "Administrator" && values.RoleIds.Contains(x.Id));
            if (!retainsAdmin && await readRepository.IsActiveAdministratorAsync(request.Id, cancellationToken) &&
                !await readRepository.HasOtherActiveAdministratorAsync(request.Id, cancellationToken))
                return CommandResult<Unit>.Failure("user.last_administrator_cannot_be_deactivated", "Last active administrator must remain active and retain the Administrator role.");


            if (string.IsNullOrWhiteSpace(values.Login))
                return CommandResult<Unit>.Failure(
                    "user.login_required",
                    "User login is required.");

            if (!UserValidation.IsValidLogin(values.Login))
                return CommandResult<Unit>.Failure(
                    "user.login_invalid",
                    "User login must be between 3 and 100 characters.");

            if (string.IsNullOrWhiteSpace(values.Email))
                return CommandResult<Unit>.Failure(
                    "user.email_required",
                    "User email is required.");

            if (!UserValidation.IsValidEmail(values.Email))
                return CommandResult<Unit>.Failure(
                    "user.email_invalid",
                    "User email format is invalid.");

            if (values.IsActive && !UserValidation.HasAtLeastOneRole(values.RoleIds))
                return CommandResult<Unit>.Failure(
                    "user.role_required",
                    "Active user must have at least one role.");

            if (await readRepository.ExistsByLoginAsync(values.Login, request.Id, cancellationToken))
                return CommandResult<Unit>.Failure("user.login_already_exists", "A user with this login already exists.");
            if (await readRepository.ExistsByEmailAsync(values.Email, request.Id, cancellationToken))
                return CommandResult<Unit>.Failure("user.email_already_exists", "A user with this email already exists.");

            await writeRepository.UpdateAsync(values, cancellationToken);
            await writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
