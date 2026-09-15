using CorporateStarter.Application.Common.Interfaces.Repositories.Auth;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Application.Common.Interfaces.Security.Mfa;
using CorporateStarter.Application.Common.Security.Mfa;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Shared.Dtos.Auth;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserAuthRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ISecurityEventRepository _securityEventRepository;
        private readonly RefreshTokenOptions _refreshTokenOptions;
        private readonly ILoginAttemptRepository _loginAttemptRepository;
        private readonly LoginAttemptOptions _loginAttemptOptions;
        private readonly IMfaPolicyService _mfaPolicyService;
        private readonly IMfaChallengeService _mfaChallengeService;

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAuthTransactionFactory _authTransactionFactory;

        public AuthService(
            IUserAuthRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IRefreshTokenService refreshTokenService,
            IRefreshTokenRepository refreshTokenRepository,
            ISecurityEventRepository securityEventRepository,
            RefreshTokenOptions refreshTokenOptions,
            ILoginAttemptRepository loginAttemptRepository,
            LoginAttemptOptions loginAttemptOptions,
            IMfaPolicyService mfaPolicyService,
            IMfaChallengeService mfaChallengeService,
            ICorrelationIdProvider correlationIdProvider,
            IAuthTransactionFactory authTransactionFactory)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _refreshTokenService = refreshTokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _securityEventRepository = securityEventRepository;
            _refreshTokenOptions = refreshTokenOptions;
            _correlationIdProvider = correlationIdProvider;
            _loginAttemptRepository = loginAttemptRepository;
            _loginAttemptOptions = loginAttemptOptions;
            _mfaPolicyService = mfaPolicyService;
            _mfaChallengeService = mfaChallengeService;
            _authTransactionFactory = authTransactionFactory;
        }

        public async Task<AuthLoginResult?> LoginAsync(
            LoginRequest request,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var loginIdentifierHash = CreateLoginIdentifierHash(request.Login);

            var loginAttemptState = await _loginAttemptRepository.GetByLoginIdentifierHashAsync(
                loginIdentifierHash,
                cancellationToken);

            if (loginAttemptState?.LockedUntilUtc > now)
            {
                await AddSecurityEventAsync(
                    "LoginBlockedByLockout",
                    "Medium",
                    "Failure",
                    loginAttemptState.UserId,
                    null,
                    null,
                    null,
                    ipAddress,
                    userAgent,
                    new
                    {
                        LockedUntilUtc = loginAttemptState.LockedUntilUtc
                    },
                    cancellationToken);

                await _loginAttemptRepository.SaveChangesAsync(cancellationToken);

                return null;
            }


            var user = await _userRepository.GetByLoginOrEmailAsync(
                request.Login,
                cancellationToken);

            if (user is null)
            {
                loginAttemptState = await RegisterFailedLoginAttemptAsync(
                    loginAttemptState,
                    loginIdentifierHash,
                    null,
                    ipAddress,
                    userAgent,
                    now,
                    cancellationToken);

                await AddSecurityEventAsync(
                    "LoginFailed",
                    "Medium",
                    "Failure",
                    null,
                    request.Login,
                    null,
                    null,
                    ipAddress,
                    userAgent,
                    new { Reason = "UserNotFound" },
                    cancellationToken);

                await ApplyProgressiveLoginDelayAsync(loginAttemptState, cancellationToken);
                await _loginAttemptRepository.SaveChangesAsync(cancellationToken);

                await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

                return null;
            }

            var passwordValid = _passwordHasher.Verify(
                request.Password,
                user.PasswordHash);

            if (!passwordValid)
            {

                loginAttemptState = await RegisterFailedLoginAttemptAsync(
                    loginAttemptState,
                    loginIdentifierHash,
                    user.Id,
                    ipAddress,
                    userAgent,
                    now,
                    cancellationToken);

                await AddSecurityEventAsync(
                    "LoginFailed",
                    "Medium",
                    "Failure",
                    user.Id,
                    user.Email,
                    null,
                    null,
                    ipAddress,
                    userAgent,
                    new { Reason = "InvalidPassword" },
                    cancellationToken);

                await ApplyProgressiveLoginDelayAsync(loginAttemptState, cancellationToken);
                await _loginAttemptRepository.SaveChangesAsync(cancellationToken);

                await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

                return null;
            }

            var profile = ToProfile(user);

            var mfaContext = new MfaEvaluationContext
            {
                UserId = user.Id,
                Login = user.Login,
                Email = user.Email,
                Roles = profile.Roles
                    .Select(x => x.Name)
                    .ToArray(),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            var mfaRequirement = await _mfaPolicyService.EvaluateRequirementAsync(
                mfaContext,
                cancellationToken);

            if (mfaRequirement is MfaRequirement.Required or MfaRequirement.EnrollmentRequired)
            {
                var challenge = await _mfaChallengeService.CreateChallengeAsync(
                    mfaContext,
                    cancellationToken);

                await AddSecurityEventAsync(
                    mfaRequirement == MfaRequirement.Required
                        ? "MfaChallengeCreated"
                        : "MfaEnrollmentRequired",
                    "Medium",
                    "Challenge",
                    user.Id,
                    user.Email,
                    null,
                    null,
                    ipAddress,
                    userAgent,
                    new
                    {
                        Requirement = mfaRequirement.ToString(),
                        ProviderType = challenge.ProviderType?.ToString()
                    },
                    cancellationToken);

                await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

                if (!challenge.Succeeded && challenge.ChallengeId.HasValue && challenge.ProviderType.HasValue)
                {
                    return AuthLoginResult.MfaRequired(
                        new MfaChallengeResponse
                        {
                            ChallengeId = challenge.ChallengeId.Value,
                            ProviderType = challenge.ProviderType.Value.ToString(),
                            PublicChallengeDataJson = challenge.PublicChallengeDataJson
                        });
                }

                return null;
            }

            var authSession = new AuthSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAtUtc = now,
                LastSeenAtUtc = now,
                CreatedByIp = ipAddress,
                UserAgent = userAgent
            };

            var refreshTokenFamily = new RefreshTokenFamily
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AuthSessionId = authSession.Id,
                CreatedAtUtc = now
            };

            var jwtToken = _jwtTokenService.CreateToken(
                profile,
                authSession.Id);

            authSession.CurrentJwtId = jwtToken.JwtId;

            var refreshToken = _refreshTokenService.GenerateToken();
            var refreshTokenHash = _refreshTokenService.HashToken(refreshToken);

            var refreshTokenExpiresAtUtc = now.AddDays(
                _refreshTokenOptions.LifetimeDays);

            await _refreshTokenRepository.AddAuthSessionAsync(
                authSession,
                cancellationToken);

            await _refreshTokenRepository.AddRefreshTokenFamilyAsync(
                refreshTokenFamily,
                cancellationToken);

            await _refreshTokenRepository.AddAsync(
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    AuthSessionId = authSession.Id,
                    RefreshTokenFamilyId = refreshTokenFamily.Id,
                    JwtId = jwtToken.JwtId,
                    TokenHash = refreshTokenHash,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = refreshTokenExpiresAtUtc,
                    CreatedByIp = ipAddress,
                    UserAgent = userAgent
                },
                cancellationToken);

            await AddSecurityEventAsync(
                "LoginSucceeded",
                "Low",
                "Success",
                user.Id,
                user.Email,
                authSession.Id,
                refreshTokenFamily.Id,
                ipAddress,
                userAgent,
                null,
                cancellationToken);

            ResetLoginAttemptState(loginAttemptState);

            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            return AuthLoginResult.Authenticated(
                new AuthSessionResult
                {
                    Response = new LoginResponse
                    {
                        AccessToken = jwtToken.AccessToken,
                        ExpiresAtUtc = jwtToken.ExpiresAtUtc,
                        User = profile
                    },
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
                });
        }

        public async Task<AuthSessionResult?> RefreshAsync(
            string refreshToken,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            await using var transaction =
                await _authTransactionFactory.BeginSerializableAsync(
                    cancellationToken);

            var result = await RefreshCoreAsync(
                refreshToken,
                ipAddress,
                userAgent,
                cancellationToken);

            // Rejection can also persist revocations and security events.
            await transaction.CommitAsync(cancellationToken);

            return result;
        }

        public async Task<AuthSessionResult?> RefreshCoreAsync(
            string refreshToken,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            var refreshTokenHash = _refreshTokenService.HashToken(refreshToken);
            var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(
                refreshTokenHash,
                cancellationToken);

            if (existingToken is null)
                return null;

            if (!existingToken.IsActive ||
                existingToken.AuthSession.IsRevoked ||
                existingToken.RefreshTokenFamily.IsRevoked)
            {
                if (!string.IsNullOrWhiteSpace(existingToken.ReplacedByTokenHash))
                {
                    await RevokeAllSessionsForUserAsync(
                        existingToken.UserId,
                        ipAddress,
                        cancellationToken);

                    MarkRefreshTokenFamilyReuse(
                        existingToken.RefreshTokenFamily,
                        ipAddress);

                    await AddSecurityEventAsync(
                        "RefreshTokenReuseDetected",
                        "Critical",
                        "Failure",
                        existingToken.UserId,
                        existingToken.User.Email,
                        existingToken.AuthSessionId,
                        existingToken.RefreshTokenFamilyId,
                        ipAddress,
                        userAgent,
                        new { Action = "RevokedAllSessions" },
                        cancellationToken);

                    await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
                }

                return null;
            }

            var profile = await GetCurrentUserAsync(existingToken.UserId, cancellationToken);

            if (profile is null)
            {
                await RevokeAllSessionsForUserAsync(
                    existingToken.UserId,
                    ipAddress,
                    cancellationToken);

                await AddSecurityEventAsync(
                    "RefreshFailedInactiveUser",
                    "High",
                    "Failure",
                    existingToken.UserId,
                    existingToken.User.Email,
                    existingToken.AuthSessionId,
                    existingToken.RefreshTokenFamilyId,
                    ipAddress,
                    userAgent,
                    null,
                    cancellationToken);

                await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

                return null;
            }

            var now = DateTime.UtcNow;

            var jwtToken = _jwtTokenService.CreateToken(
                profile,
                existingToken.AuthSessionId);

            var newRefreshToken = _refreshTokenService.GenerateToken();
            var newRefreshTokenHash = _refreshTokenService.HashToken(newRefreshToken);

            var refreshTokenExpiresAtUtc = now.AddDays(
                _refreshTokenOptions.LifetimeDays);

            var consumed = await _refreshTokenRepository.TryConsumeAsync(
                refreshTokenHash,
                newRefreshTokenHash,
                now,
                ipAddress,
                cancellationToken);

            if (!consumed)
                return null;

            existingToken.AuthSession.LastSeenAtUtc = now;
            existingToken.AuthSession.CurrentJwtId = jwtToken.JwtId;

            await _refreshTokenRepository.AddAsync(
                new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = existingToken.UserId,
                    AuthSessionId = existingToken.AuthSessionId,
                    RefreshTokenFamilyId = existingToken.RefreshTokenFamilyId,
                    JwtId = jwtToken.JwtId,
                    TokenHash = newRefreshTokenHash,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = refreshTokenExpiresAtUtc,
                    CreatedByIp = ipAddress,
                    UserAgent = userAgent
                },
                cancellationToken);

            await AddSecurityEventAsync(
                "RefreshSucceeded",
                "Low",
                "Success",
                existingToken.UserId,
                existingToken.User.Email,
                existingToken.AuthSessionId,
                existingToken.RefreshTokenFamilyId,
                ipAddress,
                userAgent,
                null,
                cancellationToken);

            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            return new AuthSessionResult
            {
                Response = new LoginResponse
                {
                    AccessToken = jwtToken.AccessToken,
                    ExpiresAtUtc = jwtToken.ExpiresAtUtc,
                    User = profile
                },
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
            };
        }

        public async Task LogoutCoreAsync(
                string? refreshToken,
                string? ipAddress,
                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var refreshTokenHash = _refreshTokenService.HashToken(refreshToken);
            var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(
                refreshTokenHash,
                cancellationToken);

            if (existingToken is null)
                return;

            await RevokeSessionAsync(
                existingToken.AuthSession,
                ipAddress,
                cancellationToken);

            if (!existingToken.RefreshTokenFamily.IsRevoked)
            {
                existingToken.RefreshTokenFamily.RevokedAtUtc = DateTime.UtcNow;
                existingToken.RefreshTokenFamily.RevokedByIp = ipAddress;
            }

            await AddSecurityEventAsync(
                "Logout",
                "Low",
                "Success",
                existingToken.UserId,
                existingToken.User.Email,
                existingToken.AuthSessionId,
                existingToken.RefreshTokenFamilyId,
                ipAddress,
                existingToken.UserAgent,
                null,
                cancellationToken);

            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task LogoutAsync(
                string? refreshToken,
                string? ipAddress,
                CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            await using var transaction =
                await _authTransactionFactory.BeginSerializableAsync(
                    cancellationToken);

            await LogoutCoreAsync(
                refreshToken,
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        public async Task LogoutAllCoreAsync(
                Guid userId,
                string? ipAddress,
                CancellationToken cancellationToken)
        {
            await RevokeAllSessionsForUserAsync(
                userId,
                ipAddress,
                cancellationToken);

            await AddSecurityEventAsync(
                "LogoutAllSessions",
                "Medium",
                "Success",
                userId,
                null,
                null,
                null,
                ipAddress,
                null,
                null,
                cancellationToken);

            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task LogoutAllAsync(
                Guid userId,
                string? ipAddress,
                CancellationToken cancellationToken)
        {
            await using var transaction =
                await _authTransactionFactory.BeginSerializableAsync(
                    cancellationToken);

            await LogoutAllCoreAsync(
                userId,
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        public async Task<UserProfileDto?> GetCurrentUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(
                userId,
                cancellationToken);

            return user is null ? null : ToProfile(user);
        }

        private async Task RevokeActiveTokensForUserAsync(
            Guid userId,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var activeTokens = await _refreshTokenRepository.GetActiveTokensByUserIdAsync(
                userId,
                cancellationToken);

            var now = DateTime.UtcNow;

            foreach (var activeToken in activeTokens)
            {
                activeToken.RevokedAtUtc = now;
                activeToken.RevokedByIp = ipAddress;
            }
        }

        private async Task AddSecurityEventAsync(
            string eventType,
            string severity,
            string outcome,
            Guid? userId,
            string? userEmail,
            Guid? authSessionId,
            Guid? refreshTokenFamilyId,
            string? ipAddress,
            string? userAgent,
            object? details,
            CancellationToken cancellationToken)
        {
            await _securityEventRepository.AddAsync(
                new SecurityEvent
                {
                    EventType = eventType,
                    Severity = severity,
                    Outcome = outcome,
                    SubjectUserId = userId,
                    SubjectUserEmail = userEmail,
                    AuthSessionId = authSessionId,
                    RefreshTokenFamilyId = refreshTokenFamilyId,
                    CorrelationId = _correlationIdProvider.CorrelationId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
                    CreatedAtUtc = DateTime.UtcNow
                },
                cancellationToken);
        }

        private static UserProfileDto ToProfile(UserAuthInfo user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                Login = user.Login,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Roles = user.Roles
            };
        }

        private async Task RevokeSessionAsync(
                AuthSession authSession,
                string? ipAddress,
                CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            if (!authSession.IsRevoked)
            {
                authSession.RevokedAtUtc = now;
                authSession.RevokedByIp = ipAddress;
            }

            foreach (var refreshTokenFamily in authSession.RefreshTokenFamilies)
            {
                if (!refreshTokenFamily.IsRevoked)
                {
                    refreshTokenFamily.RevokedAtUtc = now;
                    refreshTokenFamily.RevokedByIp = ipAddress;
                }
            }

            var activeTokens = await _refreshTokenRepository.GetActiveTokensBySessionIdAsync(
                authSession.Id,
                cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.RevokedAtUtc = now;
                activeToken.RevokedByIp = ipAddress;
            }
        }

        public async Task<bool> RevokeSessionAsync(
            Guid userId,
            Guid authSessionId,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            await using var transaction =
                await _authTransactionFactory.BeginSerializableAsync(
                    cancellationToken);

            var revoked = await RevokeSessionCoreAsync(
                userId,
                authSessionId,
                ipAddress,
                userAgent,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return revoked;
        }

        private async Task RevokeAllSessionsForUserAsync(
                Guid userId,
                string? ipAddress,
                CancellationToken cancellationToken)
        {
            var activeSessions = await _refreshTokenRepository.GetActiveSessionsByUserIdAsync(
                userId,
                cancellationToken);

            foreach (var activeSession in activeSessions)
            {
                await RevokeSessionAsync(
                    activeSession,
                    ipAddress,
                    cancellationToken);
            }
        }

        private static void MarkRefreshTokenFamilyReuse(
            RefreshTokenFamily refreshTokenFamily,
            string? ipAddress)
        {
            var now = DateTime.UtcNow;

            if (!refreshTokenFamily.IsRevoked)
            {
                refreshTokenFamily.RevokedAtUtc = now;
                refreshTokenFamily.RevokedByIp = ipAddress;
            }

            refreshTokenFamily.ReuseDetectedAtUtc = now;
            refreshTokenFamily.ReuseDetectedByIp = ipAddress;
        }

        private async Task<LoginAttemptState> RegisterFailedLoginAttemptAsync(
            LoginAttemptState? loginAttemptState,
            string loginIdentifierHash,
            Guid? userId,
            string? ipAddress,
            string? userAgent,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (loginAttemptState is null)
            {
                loginAttemptState = new LoginAttemptState
                {
                    Id = Guid.NewGuid(),
                    LoginIdentifierHash = loginIdentifierHash,
                    UserId = userId,
                    FailedAttemptCount = 0,
                    FirstFailedAtUtc = now
                };

                await _loginAttemptRepository.AddAsync(loginAttemptState, cancellationToken);
            }

            var windowStartUtc = now.AddMinutes(-_loginAttemptOptions.WindowMinutes);

            if (loginAttemptState.FirstFailedAtUtc < windowStartUtc)
            {
                loginAttemptState.FailedAttemptCount = 0;
                loginAttemptState.FirstFailedAtUtc = now;
                loginAttemptState.LockedUntilUtc = null;
            }

            loginAttemptState.UserId ??= userId;
            loginAttemptState.FailedAttemptCount++;
            loginAttemptState.LastFailedAtUtc = now;
            loginAttemptState.LastIpAddress = ipAddress;
            loginAttemptState.LastUserAgent = userAgent;

            if (loginAttemptState.FailedAttemptCount >= _loginAttemptOptions.MaxFailedAttempts)
            {
                loginAttemptState.LockedUntilUtc = now.AddMinutes(_loginAttemptOptions.LockoutMinutes);

                await AddSecurityEventAsync(
                    "LoginLocked",
                    "High",
                    "Failure",
                    loginAttemptState.UserId,
                    null,
                    null,
                    null,
                    ipAddress,
                    userAgent,
                    new
                    {
                        loginAttemptState.FailedAttemptCount,
                        loginAttemptState.LockedUntilUtc
                    },
                    cancellationToken);
            }

            return loginAttemptState;
        }

        private void ResetLoginAttemptState(LoginAttemptState? loginAttemptState)
        {
            if (loginAttemptState is null)
            {
                return;
            }

            loginAttemptState.FailedAttemptCount = 0;
            loginAttemptState.LockedUntilUtc = null;
        }

        private async Task ApplyProgressiveLoginDelayAsync(
            LoginAttemptState loginAttemptState,
            CancellationToken cancellationToken)
        {
            var exponent = Math.Min(loginAttemptState.FailedAttemptCount - 1, 10);
            var delayMilliseconds = _loginAttemptOptions.ProgressiveDelayBaseMilliseconds * (int)Math.Pow(2, exponent);

            delayMilliseconds = Math.Min(
                delayMilliseconds,
                _loginAttemptOptions.MaxProgressiveDelayMilliseconds);

            if (delayMilliseconds > 0)
            {
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }

        private static string CreateLoginIdentifierHash(string login)
        {
            var normalizedLogin = login.Trim().ToUpperInvariant();
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedLogin));

            return Convert.ToHexString(bytes);
        }

        public async Task<IReadOnlyList<AuthSessionDto>> GetActiveSessionsAsync(
            Guid userId,
            Guid? currentAuthSessionId,
            CancellationToken cancellationToken)
        {
            var sessions = await _refreshTokenRepository.GetActiveSessionsByUserIdAsync(
                userId,
                cancellationToken);

            return sessions
                .OrderByDescending(x => x.LastSeenAtUtc)
                .Select(x => new AuthSessionDto
                {
                    Id = x.Id,
                    CreatedAtUtc = x.CreatedAtUtc,
                    LastSeenAtUtc = x.LastSeenAtUtc,
                    CreatedByIp = x.CreatedByIp,
                    UserAgent = x.UserAgent,
                    DeviceName = x.DeviceName,
                    IsCurrent = currentAuthSessionId.HasValue && x.Id == currentAuthSessionId.Value
                })
                .ToList();
        }

        public async Task<bool> RevokeSessionCoreAsync(
            Guid userId,
            Guid authSessionId,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken)
        {
            var authSession = await _refreshTokenRepository.GetActiveSessionByIdForUserAsync(
                authSessionId,
                userId,
                cancellationToken);

            if (authSession is null)
            {
                return false;
            }

            await RevokeSessionAsync(authSession, ipAddress, cancellationToken);

            await AddSecurityEventAsync(
                "AuthSessionRevoked",
                "Medium",
                "Success",
                userId,
                null,
                authSession.Id,
                null,
                ipAddress,
                userAgent,
                null,
                cancellationToken);

            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}