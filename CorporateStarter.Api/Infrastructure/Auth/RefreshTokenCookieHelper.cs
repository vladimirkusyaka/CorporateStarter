using Microsoft.Extensions.Options;
using CorporateStarter.Application.Common.Security;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class RefreshTokenCookieHelper
    {
        private readonly RefreshTokenOptions _options;

        public RefreshTokenCookieHelper(IOptions<RefreshTokenOptions> options)
        {
            _options = options.Value;
        }

        public string? Read(HttpRequest request)
        {
            return request.Cookies.TryGetValue(_options.CookieName, out var token)
                ? token
                : null;
        }

        public void Append(HttpResponse response, string refreshToken, DateTimeOffset expiresAtUtc)
        {
            response.Cookies.Append(
                _options.CookieName,
                refreshToken,
                BuildCookieOptions(expiresAtUtc));
        }

        public void Delete(HttpResponse response)
        {
            response.Cookies.Delete(
                _options.CookieName,
                BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
        }

        private CookieOptions BuildCookieOptions(DateTimeOffset expiresAtUtc)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = _options.SecureCookie,
                SameSite = ParseSameSite(_options.SameSite),
                Path = "/",
                Expires = expiresAtUtc
            };
        }

        private static SameSiteMode ParseSameSite(string value)
        {
            return value.Trim().ToLowerInvariant() switch
            {
                "strict" => SameSiteMode.Strict,
                "lax" => SameSiteMode.Lax,
                "none" => SameSiteMode.None,
                _ => SameSiteMode.Strict
            };
        }
    }
}
