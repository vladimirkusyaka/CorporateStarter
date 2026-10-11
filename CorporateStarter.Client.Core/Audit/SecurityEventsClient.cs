using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Audit;

namespace CorporateStarter.Client.Core.Audit;

public sealed class SecurityEventsClient(ClientApiClient api) : TableClient<SecurityEventRowDto>(api, "/api/security-events", ClientJsonContext.Default.SecurityEventPage, x => x.Id)
{
    public override Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync(Route + "/capabilities", ClientJsonContext.Default.TableCapabilities, ct);
    public async Task<SecurityEventListItemDto> GetDetailsAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Record ID is required.", nameof(id));
        var item = await Api.GetJsonAsync($"{Route}/{id:D}", ClientJsonContext.Default.SecurityEventListItemDto, ct);
        if (item.Id != id) throw new InvalidDataException("The server returned a different security event.");
        return item;
    }
    public override Task DeleteAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException("Security events are read-only.");
}
