using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Application.Common.Results;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Security.Users.Commands;
using CorporateStarter.Application.Security.Users.Validation;
using CorporateStarter.Core.Entities.Security;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Security.Users.Handlers
{
    public sealed class ChangeUserPasswordCommandHandler(
            IUserReadRepository readRepository,
            IUserWriteRepository writeRepository,
            IPasswordHasher passwordHasher,
            IPasswordPolicyValidator passwordPolicyValidator,
            IPasswordHistoryRepository passwordHistoryRepository,
            PasswordHistoryOptions passwordHistoryOptions)
            : IRequestHandler<ChangeUserPasswordCommand, CommandResult<Unit>>
    {
        public async Task<CommandResult<Unit>> Handle(
            ChangeUserPasswordCommand request,
            CancellationToken cancellationToken)
        {
            var user = await readRepository.GetByIdAsync(request.Id, cancellationToken);

            if (user is null)
                return CommandResult<Unit>.Failure("user.not_found", "User not found.");

            var password = request.Request.NewPassword?.Trim();

            if (string.IsNullOrWhiteSpace(password))
                return CommandResult<Unit>.Failure("user.password_required", "User password is required.");

            var passwordPolicyResult = passwordPolicyValidator.Validate(
                password,
                user.Login,
                user.Email);

            if (!passwordPolicyResult.IsValid)
            {
                return CommandResult<Unit>.Failure(
                    "user.password_policy_failed",
                    string.Join(" ", passwordPolicyResult.Errors));
            }

            var recentPasswordHashes = await passwordHistoryRepository.GetRecentPasswordHashesAsync(
                    request.Id,
                    passwordHistoryOptions.HistoryCount,
                    cancellationToken);

            if (recentPasswordHashes.Any(hash => passwordHasher.Verify(password, hash)))
            {
                return CommandResult<Unit>.Failure(
                    "user.password_reused",
                    $"User password must not match the last {passwordHistoryOptions.HistoryCount} passwords.");
            }

            var passwordHash = passwordHasher.Hash(password);

            await writeRepository.ChangePasswordAsync(request.Id, passwordHash, cancellationToken);

            await passwordHistoryRepository.AddAsync(
                new PasswordHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = request.Id,
                    PasswordHash = passwordHash,
                    CreatedAtUtc = DateTime.UtcNow
                },
                cancellationToken);

            await writeRepository.SaveChangesAsync(cancellationToken);

            return CommandResult<Unit>.Success(Unit.Value);
        }
    }
}
