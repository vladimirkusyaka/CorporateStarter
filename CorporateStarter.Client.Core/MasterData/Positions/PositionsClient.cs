using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
namespace CorporateStarter.Client.Core.MasterData.Positions;

public sealed class PositionsClient(ClientApiClient api)
    : MasterDataClient<PositionListItemDto, PositionDetailsDto, CreatePositionRequest, UpdatePositionRequest>(
        api, "/api/Positions", ClientJsonContext.Default.PositionPage, ClientJsonContext.Default.Positions,
        ClientJsonContext.Default.PositionDetailsDto, ClientJsonContext.Default.CreatePositionRequest,
        ClientJsonContext.Default.UpdatePositionRequest, x => x.Id, x => x.Name is not null,
        x => x.Id, x => !string.IsNullOrWhiteSpace(x.Name))
{
    public Task<TableCapabilities> CapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync("/api/Positions/capabilities", ClientJsonContext.Default.TableCapabilities, ct);
    public override Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) => CapabilitiesAsync(ct);
}
