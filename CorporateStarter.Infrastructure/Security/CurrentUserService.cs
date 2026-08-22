using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Interfaces.Security;

namespace CorporateStarter.Infrastructure.Security
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return Guid.TryParse(value, out var userId)
                    ? userId
                    : null;
            }
        }

        public string? Login =>
            User.FindFirst(ClaimTypes.Name)?.Value;

        public string? Email =>
            User.FindFirst(ClaimTypes.Email)?.Value;

        public bool IsAuthenticated =>
            User.Identity?.IsAuthenticated == true;

        private ClaimsPrincipal User =>
            _httpContextAccessor.HttpContext?.User
            ?? new ClaimsPrincipal(new ClaimsIdentity());
    }
}
