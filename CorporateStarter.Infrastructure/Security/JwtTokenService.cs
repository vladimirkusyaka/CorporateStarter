using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Shared.Dtos.Auth;

namespace CorporateStarter.Infrastructure.Security
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        public const string PermissionClaimType = "permission";

        public const string AuthSessionIdClaimType = "auth_session_id";

        private readonly JwtOptions _jwtOptions;
        private readonly JwtSigningKeyOptions _signingKeyOptions;

        public JwtTokenService(IOptions<JwtOptions> jwtOptions, JwtSigningKeyOptions signingKeyOptions)
        {
            _jwtOptions = jwtOptions.Value;
            _signingKeyOptions = signingKeyOptions;
        }

        public JwtTokenResult CreateToken(UserProfileDto user)
        {
            return CreateToken(
                user,
                (Guid?)null);
        }

        public JwtTokenResult CreateToken(
            UserProfileDto user,
            Guid authSessionId)
        {
            return CreateToken(
                user,
                (Guid?)authSessionId);
        }

        private JwtTokenResult CreateToken(
            UserProfileDto user,
            Guid? authSessionId)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
            var jwtId = Guid.NewGuid().ToString("N");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, jwtId),
                new(JwtRegisteredClaimNames.UniqueName, user.Login),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Login),
                new(ClaimTypes.Email, user.Email)
            };

            if (authSessionId.HasValue)
            {
                claims.Add(
                    new Claim(
                        AuthSessionIdClaimType,
                        authSessionId.Value.ToString()));
            }

            claims.AddRange(
                user.Roles
                    .Select(role => role.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(role => new Claim(ClaimTypes.Role, role)));

            claims.AddRange(
                user.Roles
                    .SelectMany(role => role.Permissions)
                    .Select(permission => permission.Code)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(permission => new Claim(PermissionClaimType, permission)));

            var activeSigningKey = GetActiveSigningKey();

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(activeSigningKey.Secret))
            {
                KeyId = activeSigningKey.KeyId
            };

            var credentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            token.Header["kid"] = activeSigningKey.KeyId;

            return new JwtTokenResult
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAtUtc,
                JwtId = jwtId,
                AuthSessionId = authSessionId
            };
        }

        private JwtSigningKeyDescriptor GetActiveSigningKey()
        {
            var now = DateTime.UtcNow;

            var activeKey = _signingKeyOptions.Keys.FirstOrDefault(x =>
                x.IsEnabled
                && x.KeyId == _signingKeyOptions.ActiveKeyId
                && (x.NotBeforeUtc is null || x.NotBeforeUtc <= now)
                && (x.NotAfterUtc is null || x.NotAfterUtc > now));

            if (activeKey is null)
            {
                throw new InvalidOperationException("Active JWT signing key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(activeKey.Secret) || activeKey.Secret.Length < 32)
            {
                throw new InvalidOperationException("Active JWT signing key secret is too short.");
            }

            return activeKey;
        }
    }
}
