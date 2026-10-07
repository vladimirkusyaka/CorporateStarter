using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Users;
using CorporateStarter.Shared.Dtos.Security.Roles;
namespace CorporateStarter.Client.Core.Security.Users;

public sealed class UsersClient(ClientApiClient api)
    : TableClient<UserTableItemDto>(api, "/api/Users", ClientJsonContext.Default.UserPage, x => x.Id)
{
    public bool CanChangePassword { get; private set; }
    public async Task<UserCapabilities> CapabilitiesAsync(CancellationToken ct = default)
    {
        CanChangePassword = false;
        var result = await Api.GetJsonAsync(Route + "/capabilities", ClientJsonContext.Default.UserCapabilities, ct);
        CanChangePassword = result.CanChangePassword;
        return result;
    }
    public override async Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) =>
        (await CapabilitiesAsync(ct)).Table;
    public Task<RoleListItemDto[]> RoleOptionsAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync(Route + "/role-options", ClientJsonContext.Default.RoleOptions, ct);
    public async Task<UserDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        RequireId(id);
        var result = await Api.GetJsonAsync($"{Route}/{id:D}", ClientJsonContext.Default.UserDetailsDto, ct);
        if (result.Id != id || string.IsNullOrWhiteSpace(result.Login) || result.Roles is null)
            throw new InvalidDataException("Invalid user details.");
        return result;
    }
    public async Task<Guid> CreateAsync(CreateUserRequest request, CancellationToken ct = default) =>
        (await Api.PostJsonAsync(Route, request, ClientJsonContext.Default.CreateUserRequest,
            ClientJsonContext.Default.ClientCommandResult, ct)).CreatedId();
    public async Task<Guid> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        RequireId(id);
        (await Api.PutJsonAsync($"{Route}/{id:D}", request, ClientJsonContext.Default.UpdateUserRequest,
            ClientJsonContext.Default.ClientCommandResult, ct)).EnsureSuccess();
        return id;
    }
    public override async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        RequireId(id);
        (await Api.DeleteJsonAsync($"{Route}/{id:D}", ClientJsonContext.Default.ClientCommandResult, ct)).EnsureSuccess();
    }
    public async Task<Guid> ChangePasswordAsync(Guid id, ChangeUserPasswordRequest request, CancellationToken ct = default)
    {
        RequireId(id);
        (await Api.PutJsonAsync($"{Route}/{id:D}/password", request, ClientJsonContext.Default.ChangeUserPasswordRequest,
            ClientJsonContext.Default.ClientCommandResult, ct)).EnsureSuccess();
        return id;
    }
    private static void RequireId(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(id));
    }
}
