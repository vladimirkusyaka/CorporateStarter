using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Dtos.Auth;

namespace CorporateStarter.Application.Common.Interfaces.Security
{
     public interface IAuthService
    {
        Task<AuthLoginResult?> LoginAsync(
            LoginRequest request,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);

        Task<AuthSessionResult?> RefreshAsync(
            string refreshToken,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);

        Task LogoutAsync(
            string? refreshToken,
            string? ipAddress,
            CancellationToken cancellationToken);

        Task LogoutAllAsync(
            Guid userId,
            string? ipAddress,
            CancellationToken cancellationToken);

        Task<UserProfileDto?> GetCurrentUserAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<AuthSessionDto>> GetActiveSessionsAsync(
            Guid userId,
            Guid? currentAuthSessionId,
            CancellationToken cancellationToken);

        Task<bool> RevokeSessionAsync(
            Guid userId,
            Guid authSessionId,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);
    }
}
