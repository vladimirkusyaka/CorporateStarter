using System.ComponentModel.DataAnnotations;
using CorporateStarter.Client.Core.MasterData.Countries;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Pages.Countries;

public partial class CountryEditorDialog : RecordEditorBase<UpdateCountryRequest, CountryDetailsDto>
{
    [Inject] private CountriesClient Client { get; set; } = default!;
    [Parameter] public Guid? CountryId { get; set; }
    protected override Guid? RecordId => CountryId;
    private (string, string, string?, string?, bool?) _baseline;
    protected override bool IsDirty => (_model.Code, _model.Name, _model.NativeName, _model.PhoneCode, _model.IsActive) != _baseline;
    protected override void CaptureBaseline() => _baseline = (_model.Code, _model.Name, _model.NativeName, _model.PhoneCode, _model.IsActive);
    private bool CanChangeActive => _capabilities.ViewInactive && (_baseline.Item5 == true ? _capabilities.CanDelete : _capabilities.CanRestore);
    private bool ActiveValue { get => _model.IsActive ?? true; set => _model.IsActive = value; }
    protected override string ConflictMessage => "A country with this code or name already exists.";
    protected override Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct) => Client.GetCapabilitiesAsync(ct);
    private string? ValidateCode(string? value)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateProperty(value, new ValidationContext(_model) { MemberName = nameof(UpdateCountryRequest.Code) }, results)
            ? null : results[0].ErrorMessage;
    }
    protected override async Task<UpdateCountryRequest> ReadModelAsync(Guid id, CancellationToken ct)
    {
        var item = await Client.GetByIdAsync(id, ct);
        return new() { Code = item.Code, Name = item.Name, NativeName = item.NativeName, PhoneCode = item.PhoneCode, IsActive = item.IsActive };
    }
    protected override Task<CountryDetailsDto> WriteModelAsync(CancellationToken ct) => CountryId is { } id
        ? Client.UpdateAsync(id, new()
        {
            Code = _model.Code.Trim(),
            Name = _model.Name.Trim(),
            NativeName = Optional(_model.NativeName),
            PhoneCode = Optional(_model.PhoneCode),
            IsActive = CanChangeActive && _model.IsActive != _baseline.Item5 ? _model.IsActive : null
        }, ct)
        : Client.CreateAsync(new()
        {
            Code = _model.Code.Trim(),
            Name = _model.Name.Trim(),
            NativeName = Optional(_model.NativeName),
            PhoneCode = Optional(_model.PhoneCode)
        }, ct);
}
