using System;
using System.Text;
using System.Collections.Generic;
using CorporateStarter.Core.Entities.Security;

namespace CorporateStarter.Application.Common.Interfaces.Repositories.Auth
{
    public interface IRefreshTokenRepository
    {
        Task AddAuthSessionAsync(
            AuthSession authSession,
            CancellationToken cancellationToken);

        Task AddRefreshTokenFamilyAsync(
            RefreshTokenFamily refreshTokenFamily,
            CancellationToken cancellationToken);

        Task AddAsync(
            RefreshToken refreshToken,
            CancellationToken cancellationToken);

        Task<RefreshToken?> GetByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<AuthSession>> GetActiveSessionsByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<RefreshToken>> GetActiveTokensByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<RefreshToken>> GetActiveTokensBySessionIdAsync(
            Guid authSessionId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<RefreshToken>> GetActiveTokensByFamilyIdAsync(
            Guid refreshTokenFamilyId,
            CancellationToken cancellationToken);

        Task<AuthSession?> GetActiveSessionByIdForUserAsync(
            Guid authSessionId,
            Guid userId,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(CancellationToken cancellationToken);

        Task<bool> TryConsumeAsync(
            string tokenHash,
            string replacementTokenHash,
            DateTime nowUtc,
            string? ipAddress,
            CancellationToken cancellationToken);
    }
}
