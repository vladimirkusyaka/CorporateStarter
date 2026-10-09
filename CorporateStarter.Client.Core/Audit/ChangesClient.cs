using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Audit;
namespace CorporateStarter.Client.Core.Audit;

public sealed class ChangesClient(ClientApiClient api) : TableClient<ChangeListItemDto>(api, "/api/Audit", ClientJsonContext.Default.ChangePage, x => x.Id)
{
    public override Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync(Route + "/capabilities", ClientJsonContext.Default.TableCapabilities, ct);
    public async Task<AuditLogListItemDto> GetDetailsAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Record ID is required.", nameof(id));
        var item = await Api.GetJsonAsync($"{Route}/{id:D}", ClientJsonContext.Default.AuditLogListItemDto, ct);
        if (item.Id != id) throw new InvalidDataException("The server returned a different audit record.");
        return item;
    }
    public override Task DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException("Changes are read-only.");
}
