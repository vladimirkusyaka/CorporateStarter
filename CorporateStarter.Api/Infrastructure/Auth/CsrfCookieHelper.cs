using CorporateStarter.Application.Common.Security;
using Microsoft.Extensions.Options;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class CsrfCookieHelper
    {
        private readonly CsrfOptions _options;

        public CsrfCookieHelper(IOptions<CsrfOptions> options)
        {
            _options = options.Value;
        }

        public string? ReadCookie(HttpRequest request)
        {
            return request.Cookies.TryGetValue(_options.CookieName, out var token)
                ? token
                : null;
        }

        public string? ReadHeader(HttpRequest request)
        {
            return request.Headers.TryGetValue(_options.HeaderName, out var token)
                ? token.ToString()
                : null;
        }

        public void Append(
            HttpResponse response,
            string csrfToken,
            DateTimeOffset expiresAtUtc)
        {
            response.Cookies.Append(
                _options.CookieName,
                csrfToken,
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
                HttpOnly = false,
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
