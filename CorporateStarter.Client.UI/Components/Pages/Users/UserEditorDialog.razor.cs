using System.ComponentModel.DataAnnotations;
using CorporateStarter.Client.Core.Security.Users;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Client.UI.Components.Lookups;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Users;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Pages.Users;

public partial class UserEditorDialog : RecordEditorBase<CreateUserRequest, Guid>
{
    [Inject] private UsersClient Client { get; set; } = default!;
    [Parameter] public Guid? UserId { get; set; }
    protected override Guid? RecordId => UserId;
    private int _tab;
    private bool _validating;
    private string _confirmation = string.Empty;
    private string? _personName;
    private IReadOnlyList<SelectionOption> _options = [];
    private IReadOnlySet<Guid> _selected = new HashSet<Guid>();
    private HashSet<Guid> _baselineIds = [];
    private (string, string, string?, bool, string) _baseline;
    private bool IsSelf => UserId == Auth.Current.UserId;
    protected override bool IsDirty => (_model.Login, _model.Email, _model.DisplayName, _model.IsActive, _model.Password) != _baseline ||
        !_baselineIds.SetEquals(_selected) || _confirmation.Length > 0;
    protected override void CaptureBaseline()
    {
        _baseline = (_model.Login, _model.Email, _model.DisplayName, _model.IsActive, _model.Password);
        _baselineIds = _selected.ToHashSet();
    }
    protected override string ConflictMessage => "A user with this login or email already exists.";
    protected override async Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct)
    {
        var caps = await Client.GetCapabilitiesAsync(ct);
        if (IsEditing ? caps.CanUpdate : caps.CanCreate) await LoadOptionsAsync(ct);
        return caps;
    }
    private async Task LoadOptionsAsync(CancellationToken ct)
    {
        var roles = await Client.RoleOptionsAsync(ct);
        _options = roles.Select(x => new SelectionOption(x.Id, x.Name + (x.IsActive ? "" : " (inactive)"),
            x.IsSystemRole ? "System roles" : "Custom roles", x.IsActive)).ToArray();
    }
    protected override async Task<CreateUserRequest> ReadModelAsync(Guid id, CancellationToken ct)
    {
        await LoadOptionsAsync(ct);
        var item = await Client.GetByIdAsync(id, ct);
        _selected = item.Roles.Select(x => x.RoleId).ToHashSet();
        _options = _options.Concat(item.Roles.Where(x => !_options.Any(o => o.Id == x.RoleId))
            .Select(x => new SelectionOption(x.RoleId, x.Name + " (unavailable)", "Other roles", false))).ToArray();
        _personName = item.PersonName;
        return new() { Login = item.Login, Email = item.Email, DisplayName = item.DisplayName,
            IsActive = item.IsActive, PersonId = item.PersonId };
    }
    private void RolesChanged(IReadOnlySet<Guid> ids) => _selected = ids;
    private void ChangeTab(int index) { if (!Blocked && !_confirmDiscard && !_validating) _tab = index; }
    private static string? LoginError(string value) => value?.Trim().Length is >= 3 and <= 100 ? null : "Enter a login of 3–100 characters.";
    private static string? EmailError(string value) => !string.IsNullOrWhiteSpace(value) && new EmailAddressAttribute().IsValid(value.Trim()) ? null : "Enter a valid email address.";
    private string? ConfirmationError(string value) => value == _model.Password ? null : "Passwords do not match.";
    private async Task SaveTabbedAsync()
    {
        if (Blocked || _validating || _confirmDiscard || _frame is null) return;
        _validating = true;
        try
        {
            if (!await _frame.ValidateAsync()) { _tab = 0; return; }
            if (_model.IsActive && !_options.Any(x => x.Enabled && _selected.Contains(x.Id)))
            { _tab = 1; _error = "Select at least one active role for an active user."; return; }
            await SaveAsync();
        }
        finally { _validating = false; }
    }
    protected override Task<Guid> WriteModelAsync(CancellationToken ct) => UserId is { } id
        ? Client.UpdateAsync(id, new()
        {
            Login = _model.Login.Trim(), Email = _model.Email.Trim(), DisplayName = Optional(_model.DisplayName),
            IsActive = _model.IsActive, PersonId = _model.PersonId,
            RoleIds = _baselineIds.SetEquals(_selected) ? null : _selected.ToArray()
        }, ct)
        : Client.CreateAsync(new()
        {
            Login = _model.Login.Trim(), Email = _model.Email.Trim(), DisplayName = Optional(_model.DisplayName),
            IsActive = _model.IsActive, PersonId = _model.PersonId, Password = _model.Password, RoleIds = _selected.ToArray()
        }, ct);
}
