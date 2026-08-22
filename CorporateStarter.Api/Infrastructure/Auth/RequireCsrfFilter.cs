using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class RequireCsrfFilter : IAsyncAuthorizationFilter
    {
        private readonly CsrfCookieHelper _csrfCookieHelper;
        private readonly CsrfTokenService _csrfTokenService;

        public RequireCsrfFilter(
            CsrfCookieHelper csrfCookieHelper,
            CsrfTokenService csrfTokenService)
        {
            _csrfCookieHelper = csrfCookieHelper;
            _csrfTokenService = csrfTokenService;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var cookieToken = _csrfCookieHelper.ReadCookie(context.HttpContext.Request);
            var headerToken = _csrfCookieHelper.ReadHeader(context.HttpContext.Request);

            if (string.IsNullOrWhiteSpace(cookieToken) ||
                string.IsNullOrWhiteSpace(headerToken) ||
                !_csrfTokenService.FixedTimeEquals(cookieToken, headerToken))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }

            return Task.CompletedTask;
        }
    }
}
