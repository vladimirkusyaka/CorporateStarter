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
        IUserWriteRepository writeRepository)
        : IRequestHandler<UpdateUserCommand, CommandResult<Unit>>
    {
        public async Task<CommandResult<Unit>> Handle(
            UpdateUserCommand request,
            CancellationToken cancellationToken)
        {
            if (!await readRepository.ExistsByIdAsync(request.Id, cancellationToken))
                return CommandResult<Unit>.Failure("user.not_found", "User not found.");

            var values = request.Request.Adapt<UserWriteValues>();
            values.Id = request.Id;
            values.Normalize();

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

            await writeRepository.UpdateAsync(values, cancellationToken);
            await writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
