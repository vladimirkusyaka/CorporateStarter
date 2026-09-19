using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Common.Interfaces.Repositories.Auth;
using CorporateStarter.Core.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Auth
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly SessionIdlePolicy _idlePolicy;

        public RefreshTokenRepository(AppDbContext dbContext, SessionIdlePolicy idlePolicy)
        {
            _dbContext = dbContext;
            _idlePolicy = idlePolicy;
        }

        public async Task AddAuthSessionAsync(
            AuthSession authSession,
            CancellationToken cancellationToken)
        {
            await _dbContext.AuthSessions.AddAsync(authSession, cancellationToken);
        }

        public async Task AddRefreshTokenFamilyAsync(
            RefreshTokenFamily refreshTokenFamily,
            CancellationToken cancellationToken)
        {
            await _dbContext.RefreshTokenFamilies.AddAsync(refreshTokenFamily, cancellationToken);
        }

        public async Task AddAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken)
        {
            await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken)
        {
            return await _dbContext.RefreshTokens
                .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
                .Include(x => x.AuthSession)
                .ThenInclude(x => x.RefreshTokenFamilies)
                .Include(x => x.RefreshTokenFamily)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        }

        public async Task<IReadOnlyList<AuthSession>> GetActiveSessionsByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var sessions = _dbContext.AuthSessions
                .Include(x => x.RefreshTokenFamilies)
                .Where(x =>
                    x.UserId == userId &&
                    x.RevokedAtUtc == null);

            if (_idlePolicy.IsEnabled)
            {
                var cutoff = DateTime.UtcNow - _idlePolicy.Timeout;

                sessions = sessions.Where(x =>
                    x.LastUserActivityAtUtc > cutoff);
            }

            return await sessions
                .OrderByDescending(x => x.LastSeenAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetActiveTokensByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            return await _dbContext.RefreshTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.AuthSession.RevokedAtUtc == null &&
                    x.RefreshTokenFamily.RevokedAtUtc == null &&
                    x.RevokedAtUtc == null &&
                    x.ExpiresAtUtc > now)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetActiveTokensBySessionIdAsync(
            Guid authSessionId,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            return await _dbContext.RefreshTokens
                .Where(x =>
                    x.AuthSessionId == authSessionId &&
                    x.AuthSession.RevokedAtUtc == null &&
                    x.RefreshTokenFamily.RevokedAtUtc == null &&
                    x.RevokedAtUtc == null &&
                    x.ExpiresAtUtc > now)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetActiveTokensByFamilyIdAsync(
            Guid refreshTokenFamilyId,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            return await _dbContext.RefreshTokens
                .Where(x =>
                    x.RefreshTokenFamilyId == refreshTokenFamilyId &&
                    x.AuthSession.RevokedAtUtc == null &&
                    x.RefreshTokenFamily.RevokedAtUtc == null &&
                    x.RevokedAtUtc == null &&
                    x.ExpiresAtUtc > now)
                .ToListAsync(cancellationToken);
        }

        public Task<AuthSession?> GetActiveSessionByIdForUserAsync(
            Guid authSessionId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return _dbContext.AuthSessions
                .Include(x => x.RefreshTokenFamilies)
                .FirstOrDefaultAsync(
                    x => x.Id == authSessionId
                        && x.UserId == userId
                        && x.RevokedAtUtc == null,
                    cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> TryConsumeAsync(
            string tokenHash,
            string replacementTokenHash,
            DateTime nowUtc,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
            ArgumentException.ThrowIfNullOrWhiteSpace(replacementTokenHash);

            if (string.Equals(
                tokenHash,
                replacementTokenHash,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Replacement must differ from the consumed token.",
                    nameof(replacementTokenHash));
            }

            if (nowUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "UTC timestamp is required.",
                    nameof(nowUtc));
            }

            var transaction = _dbContext.Database.CurrentTransaction;

            if (transaction is null ||
                transaction.GetDbTransaction().IsolationLevel
                    != IsolationLevel.Serializable)
            {
                throw new InvalidOperationException(
                    "Refresh token consumption requires an active Serializable transaction.");
            }

            var affectedRows = await _dbContext.RefreshTokens
                .Where(x =>
                    x.TokenHash == tokenHash &&
                    x.RevokedAtUtc == null &&
                    x.ReplacedByTokenHash == null &&
                    x.ExpiresAtUtc > nowUtc &&
                    x.User.IsActive &&
                    x.AuthSession.RevokedAtUtc == null &&
                    x.RefreshTokenFamily.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.RevokedAtUtc,
                            (DateTime?)nowUtc)
                        .SetProperty(
                            x => x.RevokedByIp,
                            ipAddress)
                        .SetProperty(
                            x => x.ReplacedByTokenHash,
                            replacementTokenHash),
                    cancellationToken);

            return affectedRows switch
            {
                0 => false,
                1 => true,
                _ => throw new InvalidOperationException(
                    "Refresh token hash uniqueness invariant was violated.")
            };
        }
    }
}
