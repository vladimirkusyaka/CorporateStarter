using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Security.Users.Commands;
using CorporateStarter.Application.Security.Users.Models;
using CorporateStarter.Application.Security.Users.Validation;
using CorporateStarter.Core.Entities.Security;
using Mapster;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;


namespace CorporateStarter.Application.Security.Users.Handlers
{
    public sealed class CreateUserCommandHandler(
        IUserReadRepository readRepository,
        IUserWriteRepository writeRepository,
        IPasswordHasher passwordHasher,
        IPasswordPolicyValidator passwordPolicyValidator,
        IPasswordHistoryRepository passwordHistoryRepository)
        : IRequestHandler<CreateUserCommand, CommandResult<Guid>>
    {
        public async Task<CommandResult<Guid>> Handle(
            CreateUserCommand request,
            CancellationToken cancellationToken)
        {
            var values = request.Request.Adapt<UserWriteValues>();
            values.Normalize();

            if (string.IsNullOrWhiteSpace(values.Login))
                return CommandResult<Guid>.Failure(
                    "user.login_required",
                    "User login is required.");

            if (!UserValidation.IsValidLogin(values.Login))
                return CommandResult<Guid>.Failure(
                    "user.login_invalid",
                    "User login must be between 3 and 100 characters.");

            if (string.IsNullOrWhiteSpace(values.Email))
                return CommandResult<Guid>.Failure(
                    "user.email_required",
                    "User email is required.");

            if (!UserValidation.IsValidEmail(values.Email))
                return CommandResult<Guid>.Failure(
                    "user.email_invalid",
                    "User email format is invalid.");

            if (string.IsNullOrWhiteSpace(values.Password))
                return CommandResult<Guid>.Failure(
                    "user.password_required",
                    "User password is required.");

            var passwordPolicyResult = passwordPolicyValidator.Validate(
                values.Password,
                values.Login,
                values.Email);

            if (!passwordPolicyResult.IsValid)
            {
                return CommandResult<Guid>.Failure(
                    "user.password_policy_failed",
                    string.Join(" ", passwordPolicyResult.Errors));
            }

            if (values.IsActive && !UserValidation.HasAtLeastOneRole(values.RoleIds))
                return CommandResult<Guid>.Failure(
                    "user.role_required",
                    "Active user must have at least one role.");

            if (!await readRepository.AllRolesExistAsync(values.RoleIds, cancellationToken))
                return CommandResult<Guid>.Failure("user.roles_not_found", "One or more roles do not exist.");

            var passwordHash = passwordHasher.Hash(values.Password);

            var id = await writeRepository.AddAsync(values, passwordHash, cancellationToken);
            await passwordHistoryRepository.AddAsync(
                        new PasswordHistory
                        {
                            Id = Guid.NewGuid(),
                            UserId = id,
                            PasswordHash = passwordHash,
                            CreatedAtUtc = DateTime.UtcNow
                        },
                        cancellationToken);
            await writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Guid>.Success(id);
        }
    }
}
