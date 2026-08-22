using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using CorporateStarter.Application.Common.Interfaces.Repositories.Auth;
using CorporateStarter.Core.Entities.Security;

namespace CorporateStarter.Infrastructure.Persistence.Repositories.Auth
{
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _dbContext;

        public RefreshTokenRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
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
            return await _dbContext.AuthSessions
                .Include(x => x.RefreshTokenFamilies)
                .Where(x =>
                    x.UserId == userId &&
                    x.RevokedAtUtc == null)
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
    }
}
