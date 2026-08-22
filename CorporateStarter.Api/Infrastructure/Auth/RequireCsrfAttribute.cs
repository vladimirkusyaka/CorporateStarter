using Microsoft.AspNetCore.Mvc;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class RequireCsrfAttribute : TypeFilterAttribute
    {
        public RequireCsrfAttribute()
            : base(typeof(RequireCsrfFilter))
        {
        }
    }
}
