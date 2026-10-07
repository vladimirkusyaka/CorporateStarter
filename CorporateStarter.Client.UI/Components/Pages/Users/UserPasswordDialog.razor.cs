using CorporateStarter.Client.Core.Security.Users;
using CorporateStarter.Client.UI.Components.Editors;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Users;
using Microsoft.AspNetCore.Components;
namespace CorporateStarter.Client.UI.Components.Pages.Users;

public partial class UserPasswordDialog : RecordEditorBase<ChangeUserPasswordRequest, Guid>
{
    [Inject] private UsersClient Client { get; set; } = default!;
    [Parameter] public Guid UserId { get; set; }
    protected override Guid? RecordId => UserId;
    private string _confirmation = string.Empty;
    private string _login = string.Empty;
    protected override bool IsDirty => _model.NewPassword.Length > 0 || _confirmation.Length > 0;
    protected override void CaptureBaseline() { }
    protected override string ConflictMessage => "The password could not be changed. Please reload the user.";
    protected override async Task<TableCapabilities> LoadCapabilitiesAsync(CancellationToken ct)
    {
        var caps = await Client.CapabilitiesAsync(ct);
        return caps.Table with { CanUpdate = caps.CanChangePassword };
    }
    protected override async Task<ChangeUserPasswordRequest> ReadModelAsync(Guid id, CancellationToken ct)
    {
        _login = (await Client.GetByIdAsync(id, ct)).Login;
        return new();
    }
    private string? ConfirmationError(string value) => value == _model.NewPassword ? null : "Passwords do not match.";
    protected override Task<Guid> WriteModelAsync(CancellationToken ct) => Client.ChangePasswordAsync(UserId, _model, ct);
}
