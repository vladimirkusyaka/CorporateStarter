using CorporateStarter.Application.Common.Interfaces.Security;

namespace CorporateStarter.Api.Infrastructure.Diagnostics
{
    public sealed class CorrelationIdProvider : ICorrelationIdProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? CorrelationId =>
            _httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] as string;
    }
}
