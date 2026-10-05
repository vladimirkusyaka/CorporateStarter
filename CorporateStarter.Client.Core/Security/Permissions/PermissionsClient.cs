using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Permissions;

namespace CorporateStarter.Client.Core.Security.Permissions;

public sealed class PermissionsClient(ClientApiClient api)
    : TableClient<PermissionListItemDto>(api, "/api/Permissions", ClientJsonContext.Default.PermissionPage, x => x.Id)
{
    public override Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync(Route + "/capabilities", ClientJsonContext.Default.TableCapabilities, ct);

    public override Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        throw new NotSupportedException("The system permission catalog is read-only.");
}
