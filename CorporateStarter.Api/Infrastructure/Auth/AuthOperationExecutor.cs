using CorporateStarter.Application.Common.Interfaces.Security;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class AuthOperationExecutor
    {
        private const int MaxAttempts = 3;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuthOperationExecutor> _logger;

        public AuthOperationExecutor(
            IServiceScopeFactory scopeFactory,
            ILogger<AuthOperationExecutor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(
            Func<IAuthService, CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await ExecuteAttemptAsync(
                        operation,
                        cancellationToken);
                }
                catch (Exception exception) when (
                    !cancellationToken.IsCancellationRequested &&
                    IsRetryableConflict(exception))
                {
                    if (attempt == MaxAttempts)
                    {
                        _logger.LogWarning(
                            "Auth transaction conflicts exhausted {MaxAttempts} attempts.",
                            MaxAttempts);

                        throw new AuthOperationContentionException(exception);
                    }

                    _logger.LogInformation(
                        "Auth transaction conflict; retrying after attempt {Attempt}.",
                        attempt);

                    var delayMs =
                        50 * (1 << (attempt - 1)) +
                        Random.Shared.Next(0, 51);

                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            throw new InvalidOperationException(
                "Unreachable auth retry state.");
        }

        public async Task ExecuteAsync(
            Func<IAuthService, CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);

            await ExecuteAsync<bool>(
                async (service, token) =>
                {
                    await operation(service, token);
                    return true;
                },
                cancellationToken);
        }

        private async Task<T> ExecuteAttemptAsync<T>(
            Func<IAuthService, CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var service =
                scope.ServiceProvider.GetRequiredService<IAuthService>();

            return await operation(service, cancellationToken);
        }

        private static bool IsRetryableConflict(Exception exception)
        {
            var postgresException = exception switch
            {
                PostgresException postgres => postgres,

                DbUpdateException
                {
                    InnerException: PostgresException postgres
                } => postgres,

                InvalidOperationException
                {
                    InnerException: PostgresException postgres
                } => postgres,

                _ => null
            };

            return postgresException?.SqlState is "40001" or "40P01";
        }
    }
}
