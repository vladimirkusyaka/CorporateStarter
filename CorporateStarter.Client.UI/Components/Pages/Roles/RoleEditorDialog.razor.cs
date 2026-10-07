using CorporateStarter.Client.Core.Security.Roles;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Client.UI.Components.Lookups;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Roles;
using CorporateStarter.Shared.Dtos.Permissions;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Pages.Roles;

public partial class RoleEditorDialog : RecordEditorBase<UpdateRoleRequest, Guid>
{
    [Inject] private RolesClient Client { get; set; } = default!;
    [Parameter] public Guid? RoleId { get; set; }
    protected override Guid? RecordId => RoleId;
    private int _tab;
    private bool _validating;
    private void ChangeTab(int index)
    {
        if (!Blocked && !_confirmDiscard && !_validating && index is 0 or 1)
            _tab = index;
    }
    private async Task SaveTabbedAsync()
    {
        if (Blocked || _system || _confirmDiscard || _validating || _frame is null) return;
        _validating = true;
        try
        {
            // KeepPanelsAlive keeps both panels registered with the shared form.
            // Reveal invalid fields even when Save was pressed on Permissions.
            if (!await _frame.ValidateAsync())
            {
                _tab = 0;
                return;
            }
            await SaveAsync();
        }
        finally { _validating = false; }
    }
    private bool _managePermissions, _system;
    private IReadOnlyList<SelectionOption> _options = [];
    private IReadOnlySet<Guid> _selected = new HashSet<Guid>();
    private HashSet<Guid> _baselineIds = [];
    private (string, string?, bool) _baseline;
    protected override bool IsDirty => (_model.Name, _model.Description, _model.IsActive) != _baseline || !_baselineIds.SetEquals(_selected);
    protected override void CaptureBaseline()
    {
        _baseline = (_model.Name, _model.Description, _model.IsActive);
        _baselineIds = _selected.ToHashSet();
    }
    protected override string ConflictMessage => "A role with this name already exists.";
    protected override async Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct)
    {
        var capabilities = await Client.CapabilitiesAsync(ct);
        _managePermissions = capabilities.CanManagePermissions;
        if (_managePermissions) _options = (await Client.PermissionOptionsAsync(ct)).Select(Option).ToArray();
        return capabilities.Table;
    }
    private static SelectionOption Option(PermissionListItemDto p) =>
        new(p.Id, p.Name + (p.IsActive ? "" : " (inactive)"), p.Group, p.IsActive);
    private void PermissionsChanged(IReadOnlySet<Guid> ids) => _selected = ids;
    protected override async Task<UpdateRoleRequest> ReadModelAsync(Guid id, CancellationToken ct)
    {
        var item = await Client.GetByIdAsync(id, ct);
        _system = item.IsSystemRole || item.Name == "Administrator";
        _selected = item.Permissions.Select(x => x.Id).ToHashSet();
        _options = _managePermissions
            ? _options.Concat(item.Permissions.Where(p => !_options.Any(o => o.Id == p.Id)).Select(Option)).ToArray()
            : item.Permissions.Select(Option).ToArray();
        return new() { Name = item.Name, Description = item.Description, IsActive = item.IsActive };
    }
    protected override Task<Guid> WriteModelAsync(CancellationToken ct)
    {
        if (_system) throw new InvalidOperationException("System roles are read-only.");
        return RoleId is { } id
            ? Client.UpdateAsync(id, new()
            {
                Name = _model.Name.Trim(),
                Description = Optional(_model.Description),
                IsActive = _model.IsActive,
                PermissionIds = _managePermissions && !_baselineIds.SetEquals(_selected) ? _selected.ToArray() : null
            }, ct)
            : Client.CreateAsync(new()
            {
                Name = _model.Name.Trim(),
                Description = Optional(_model.Description),
                IsActive = _model.IsActive,
                PermissionIds = _managePermissions ? _selected.ToArray() : []
            }, ct);
    }
}
