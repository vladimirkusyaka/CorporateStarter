using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
namespace CorporateStarter.Api.Security;

public sealed class EntityAccess(IHttpContextAccessor context) : IEntityAccess
{
    public TableCapabilities For(string resource) => EntityPermissions.Capabilities(resource,
        code => context.HttpContext?.User.HasClaim("permission", code) == true);
}
