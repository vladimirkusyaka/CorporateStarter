using CorporateStarter.Client.Core.MasterData.Positions;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Pages.Positions;

public partial class PositionEditorDialog : RecordEditorBase<UpdatePositionRequest, PositionDetailsDto>
{
    [Inject] private PositionsClient Client { get; set; } = default!;
    [Parameter] public Guid? PositionId { get; set; }
    protected override Guid? RecordId => PositionId;
    private (string, string?, bool) _baseline;
    protected override bool IsDirty => (_model.Name, _model.Description, _model.IsActive) != _baseline;
    protected override void CaptureBaseline() => _baseline = (_model.Name, _model.Description, _model.IsActive);
    protected override string ConflictMessage => "A position with this name already exists.";
    protected override Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct) => Client.GetCapabilitiesAsync(ct);
    protected override async Task<UpdatePositionRequest> ReadModelAsync(Guid id, CancellationToken ct)
    {
        var item = await Client.GetByIdAsync(id, ct);
        return new() { Name = item.Name, Description = item.Description, IsActive = item.IsActive };
    }
    protected override Task<PositionDetailsDto> WriteModelAsync(CancellationToken ct) => PositionId is { } id
        ? Client.UpdateAsync(id, new() { Name = _model.Name.Trim(), Description = Optional(_model.Description), IsActive = _model.IsActive }, ct)
        : Client.CreateAsync(new() { Name = _model.Name.Trim(), Description = Optional(_model.Description) }, ct);
}
