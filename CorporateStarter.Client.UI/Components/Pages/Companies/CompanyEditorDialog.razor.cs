using CorporateStarter.Client.Core.Directory.Companies;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Pages.Companies;

public partial class CompanyEditorDialog : RecordEditorBase<UpdateCompanyRequest, CompanyDetailsDto>
{
    [Inject] private CompaniesClient Client { get; set; } = default!;
    [Parameter] public Guid? CompanyId { get; set; }
    protected override Guid? RecordId => CompanyId;
    private LookupOption? _city;
    private int _tab;
    private bool _validating;
    private (string?, string, string?, string?, string?, string?, string?, string?, string?, string?, string?, string?, Guid?, bool) _baseline;
    protected override bool IsDirty => (_model.Code, _model.Name, _model.LegalName, _model.TaxNumber, _model.VatId, _model.Email, _model.Phone, _model.Website, _model.Street, _model.HouseNumber, _model.PostalCode, _model.Description, _model.CityId, _model.IsActive) != _baseline;
    protected override void CaptureBaseline() => _baseline = (_model.Code, _model.Name, _model.LegalName, _model.TaxNumber, _model.VatId, _model.Email, _model.Phone, _model.Website, _model.Street, _model.HouseNumber, _model.PostalCode, _model.Description, _model.CityId, _model.IsActive);
    protected override string ConflictMessage => "A company with this name or code already exists.";
    protected override string SaveNotFoundMessage => "The company or selected city no longer exists. Reload the form or choose another city.";
    protected override Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct) => Client.GetCapabilitiesAsync(ct);
    private void CityChanged(LookupOption? city) { _city = city; _model.CityId = city?.Id; }
    private void ChangeTab(int index) { if (!Blocked && !_validating && !_confirmDiscard && index is >= 0 and <= 2) _tab = index; }
    private async Task SaveTabbedAsync()
    {
        if (Blocked || _validating || _confirmDiscard || _frame is null) return;
        _validating = true;
        try
        {
            if (!await _frame.ValidateAsync()) { _tab = 0; return; }
            await SaveAsync();
        }
        finally { _validating = false; }
    }
    protected override async Task<UpdateCompanyRequest> ReadModelAsync(Guid id, CancellationToken ct)
    {
        var item = await Client.GetByIdAsync(id, ct);
        _city = item.CityId is { } cityId ? new(cityId, item.CityName + " — " + item.CountryName) : null;
        return new()
        {
            Code = item.Code,
            Name = item.Name,
            LegalName = item.LegalName,
            TaxNumber = item.TaxNumber,
            VatId = item.VatId,
            Email = item.Email,
            Phone = item.Phone,
            Website = item.Website,
            Street = item.Street,
            HouseNumber = item.HouseNumber,
            PostalCode = item.PostalCode,
            Description = item.Description,
            CityId = item.CityId,
            IsActive = item.IsActive
        };
    }
    protected override Task<CompanyDetailsDto> WriteModelAsync(CancellationToken ct) => CompanyId is { } id
        ? Client.UpdateAsync(id, new()
        {
            Code = Optional(_model.Code),
            Name = _model.Name.Trim(),
            LegalName = Optional(_model.LegalName),
            TaxNumber = Optional(_model.TaxNumber),
            VatId = Optional(_model.VatId),
            Email = Optional(_model.Email),
            Phone = Optional(_model.Phone),
            Website = Optional(_model.Website),
            Street = Optional(_model.Street),
            HouseNumber = Optional(_model.HouseNumber),
            PostalCode = Optional(_model.PostalCode),
            Description = Optional(_model.Description),
            CityId = _model.CityId,
            IsActive = _model.IsActive
        }, ct)
        : Client.CreateAsync(new()
        {
            Code = Optional(_model.Code),
            Name = _model.Name.Trim(),
            LegalName = Optional(_model.LegalName),
            TaxNumber = Optional(_model.TaxNumber),
            VatId = Optional(_model.VatId),
            Email = Optional(_model.Email),
            Phone = Optional(_model.Phone),
            Website = Optional(_model.Website),
            Street = Optional(_model.Street),
            HouseNumber = Optional(_model.HouseNumber),
            PostalCode = Optional(_model.PostalCode),
            Description = Optional(_model.Description),
            CityId = _model.CityId
        }, ct);
}
