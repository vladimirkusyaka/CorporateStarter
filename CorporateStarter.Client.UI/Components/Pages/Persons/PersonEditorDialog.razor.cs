using CorporateStarter.Client.Core.Directory.Persons;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Persons;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace CorporateStarter.Client.UI.Components.Pages.Persons;

public partial class PersonEditorDialog : RecordEditorBase<UpdatePersonRequest, PersonDetailsDto>
{
    [Inject] private PersonsClient Client { get; set; } = default!;
    [Parameter] public Guid? PersonId { get; set; }
    protected override Guid? RecordId => PersonId;
    private LookupOption? _company, _position;
    private int _tab;
    private bool _validating;
    private (string?, string, string?, string?, string?, string?, string?, DateOnly?, Guid?, Guid?, bool) _baseline;
    protected override bool IsDirty => (_model.Code, _model.FirstName, _model.MiddleName, _model.LastName, _model.Email, _model.Phone, _model.Description, _model.DateOfBirth, _model.CompanyId, _model.PositionId, _model.IsActive) != _baseline;
    protected override void CaptureBaseline() => _baseline = (_model.Code, _model.FirstName, _model.MiddleName, _model.LastName, _model.Email, _model.Phone, _model.Description, _model.DateOfBirth, _model.CompanyId, _model.PositionId, _model.IsActive);
    // DateOnly is kept at the API boundary; the calendar does not convert time zones.
    private DateTime? BirthDate
    {
        get => _model.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
        set => _model.DateOfBirth = value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }
    protected override string ConflictMessage => "The record conflicts with existing data. Reload it and try again.";
    protected override string SaveNotFoundMessage => "The person or a selected company/position no longer exists. Reload the form or clear the selection.";
    protected override Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct) => Client.GetCapabilitiesAsync(ct);
    private void CompanyChanged(LookupOption? value) { _company = value; _model.CompanyId = value?.Id; }
    private void PositionChanged(LookupOption? value) { _position = value; _model.PositionId = value?.Id; }
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
    protected override async Task<UpdatePersonRequest> ReadModelAsync(Guid id, CancellationToken ct)
    {
        var item = await Client.GetByIdAsync(id, ct);
        _company = item.CompanyId is { } companyId ? new(companyId, item.CompanyName ?? "Company") : null;
        _position = item.PositionId is { } positionId ? new(positionId, item.PositionName ?? "Position") : null;
        return new()
        {
            Code = item.Code,
            FirstName = item.FirstName,
            MiddleName = item.MiddleName,
            LastName = item.LastName,
            Email = item.Email,
            Phone = item.Phone,
            Description = item.Description,
            DateOfBirth = item.DateOfBirth,
            CompanyId = item.CompanyId,
            PositionId = item.PositionId,
            IsActive = item.IsActive
        };
    }
    protected override Task<PersonDetailsDto> WriteModelAsync(CancellationToken ct) => PersonId is { } id
        ? Client.UpdateAsync(id, new()
        {
            Code = Optional(_model.Code),
            FirstName = _model.FirstName.Trim(),
            MiddleName = Optional(_model.MiddleName),
            LastName = Optional(_model.LastName),
            Email = Optional(_model.Email),
            Phone = Optional(_model.Phone),
            Description = Optional(_model.Description),
            DateOfBirth = _model.DateOfBirth,
            CompanyId = _model.CompanyId,
            PositionId = _model.PositionId,
            IsActive = _model.IsActive
        }, ct)
        : Client.CreateAsync(new()
        {
            Code = Optional(_model.Code),
            FirstName = _model.FirstName.Trim(),
            MiddleName = Optional(_model.MiddleName),
            LastName = Optional(_model.LastName),
            Email = Optional(_model.Email),
            Phone = Optional(_model.Phone),
            Description = Optional(_model.Description),
            DateOfBirth = _model.DateOfBirth,
            CompanyId = _model.CompanyId,
            PositionId = _model.PositionId
        }, ct);
}
