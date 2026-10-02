using CorporateStarter.Client.Core.MasterData.Cities;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Pages.Cities;

public partial class CityEditorDialog : RecordEditorBase<UpdateCityRequest, CityDetailsDto>
{
    [Inject] private CitiesClient Client { get; set; } = default!;
    [Parameter] public Guid? CityId { get; set; }
    protected override Guid? RecordId => CityId;
    private LookupOption? _country;
    private (string, string?, Guid, bool) _baseline;
    protected override bool IsDirty => (_model.Name, _model.Region, _model.CountryId, _model.IsActive) != _baseline;
    protected override void CaptureBaseline() => _baseline = (_model.Name, _model.Region, _model.CountryId, _model.IsActive);
    protected override string ConflictMessage => "A city with this name and region already exists in the selected country.";
    protected override string SaveNotFoundMessage => "The city or selected country no longer exists. Reload the record or select another country.";
    protected override Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct) => Client.GetCapabilitiesAsync(ct);
    private void CountryChanged(LookupOption? option)
    {
        _country = option;
        _model.CountryId = option?.Id ?? Guid.Empty;
    }
    protected override async Task<UpdateCityRequest> ReadModelAsync(Guid id, CancellationToken ct)
    {
        var item = await Client.GetByIdAsync(id, ct);
        _country = new(item.CountryId, item.CountryCode + " — " + item.CountryName);
        return new() { Name = item.Name, Region = item.Region, CountryId = item.CountryId, IsActive = item.IsActive };
    }
    protected override Task<CityDetailsDto> WriteModelAsync(CancellationToken ct) => CityId is { } id
        ? Client.UpdateAsync(id, new() { Name = _model.Name.Trim(), Region = Optional(_model.Region), CountryId = _model.CountryId, IsActive = _model.IsActive }, ct)
        : Client.CreateAsync(new() { Name = _model.Name.Trim(), Region = Optional(_model.Region), CountryId = _model.CountryId }, ct);
}
