using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Security;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace CorporateStarter.Tests.Integration.Auth
{
    public sealed class JwtSigningKeyRotationTests
    {
        private const string Issuer = "CorporateStarter.Tests";
        private const string Audience = "CorporateStarter.Tests";

        [Fact]
        public void CreateToken_UsesActiveSigningKeyId()
        {
            var service = CreateJwtTokenService(
                activeKeyId: "key-2",
                keys:
                [
                    CreateKey("key-1", enabled: true),
                CreateKey("key-2", enabled: true)
                ]);

            var token = service.CreateToken(CreateUserProfile());

            var jwt = new JwtSecurityTokenHandler()
                .ReadJwtToken(token.AccessToken);

            Assert.Equal("key-2", jwt.Header.Kid);
        }

        [Fact]
        public void ValidateToken_WithEnabledSigningKey_Succeeds()
        {
            var key = CreateKey("key-1", enabled: true);

            var service = CreateJwtTokenService(
                activeKeyId: "key-1",
                keys: [key]);

            var token = service.CreateToken(CreateUserProfile());

            var principal = ValidateToken(
                token.AccessToken,
                keys: [key]);

            Assert.Equal(
                "admin",
                principal.FindFirstValue(ClaimTypes.Name));
        }

        [Fact]
        public void ValidateToken_WithDisabledSigningKey_Fails()
        {
            var enabledAtIssueTime = CreateKey("key-1", enabled: true);

            var service = CreateJwtTokenService(
                activeKeyId: "key-1",
                keys: [enabledAtIssueTime]);

            var token = service.CreateToken(CreateUserProfile());

            var disabledAtValidationTime = CreateKey("key-1", enabled: false);

            Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
                ValidateToken(
                    token.AccessToken,
                    keys: [disabledAtValidationTime]));
        }

        private static JwtTokenService CreateJwtTokenService(
            string activeKeyId,
            IReadOnlyList<JwtSigningKeyDescriptor> keys)
        {
            var jwtOptions = Options.Create(
                new JwtOptions
                {
                    Issuer = Issuer,
                    Audience = Audience,
                    ExpirationMinutes = 5
                });

            var signingKeyOptions = new JwtSigningKeyOptions
            {
                ActiveKeyId = activeKeyId,
                Keys = keys.ToList()
            };

            return new JwtTokenService(
                jwtOptions,
                signingKeyOptions);
        }

        private static ClaimsPrincipal ValidateToken(
            string accessToken,
            IReadOnlyList<JwtSigningKeyDescriptor> keys)
        {
            var handler = new JwtSecurityTokenHandler();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Issuer,

                ValidateAudience = true,
                ValidAudience = Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = (_, _, kid, _) =>
                {
                    if (string.IsNullOrWhiteSpace(kid))
                    {
                        return [];
                    }

                    var now = DateTime.UtcNow;

                    return keys
                        .Where(x =>
                            x.IsEnabled
                            && x.KeyId == kid
                            && !string.IsNullOrWhiteSpace(x.Secret)
                            && (x.NotBeforeUtc is null || x.NotBeforeUtc <= now)
                            && (x.NotAfterUtc is null || x.NotAfterUtc > now))
                        .Select(x => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(x.Secret))
                        {
                            KeyId = x.KeyId
                        });
                },

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            return handler.ValidateToken(
                accessToken,
                parameters,
                out _);
        }

        private static JwtSigningKeyDescriptor CreateKey(
            string keyId,
            bool enabled)
        {
            return new JwtSigningKeyDescriptor
            {
                KeyId = keyId,
                Secret = $"{keyId}-TEST_SIGNING_SECRET_32_BYTES_MINIMUM_VALUE",
                IsEnabled = enabled
            };
        }

        private static UserProfileDto CreateUserProfile()
        {
            return new UserProfileDto
            {
                Id = Guid.NewGuid(),
                Login = "admin",
                Email = "admin@corporatestarter.test",
                Roles =
                [
                    new UserRoleProfileDto
                {
                    Name = "Administrator",
                    Permissions =
                    [
                        new UserPermissionProfileDto
                        {
                            Code = "users.read"
                        }
                    ]
                }
                ]
            };
        }
    }
}
