using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Client.Core.Tables;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Roles;
using CorporateStarter.Shared.Dtos.Permissions;
namespace CorporateStarter.Client.Core.Security.Roles;

public sealed class RolesClient(ClientApiClient api)
    : TableClient<RoleListItemDto>(api, "/api/Roles", ClientJsonContext.Default.RolePage, x => x.Id)
{
    public Task<RoleCapabilities> CapabilitiesAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync(Route + "/capabilities", ClientJsonContext.Default.RoleCapabilities, ct);
    public override async Task<TableCapabilities> GetCapabilitiesAsync(CancellationToken ct = default) =>
        (await CapabilitiesAsync(ct)).Table;
    public Task<PermissionListItemDto[]> PermissionOptionsAsync(CancellationToken ct = default) =>
        Api.GetJsonAsync(Route + "/permission-options", ClientJsonContext.Default.PermissionOptions, ct);
    public async Task<RoleDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        RequireId(id);
        var result = await Api.GetJsonAsync($"{Route}/{id:D}", ClientJsonContext.Default.RoleDetailsDto, ct);
        if (result.Id != id || string.IsNullOrWhiteSpace(result.Name) || result.Permissions is null)
            throw new InvalidDataException("Invalid role details.");
        return result;
    }
    public async Task<Guid> CreateAsync(CreateRoleRequest request, CancellationToken ct = default) =>
        (await Api.PostJsonAsync(Route, request, ClientJsonContext.Default.CreateRoleRequest,
            ClientJsonContext.Default.ClientCommandResult, ct)).CreatedId();
    public async Task<Guid> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        RequireId(id);
        (await Api.PutJsonAsync($"{Route}/{id:D}", request, ClientJsonContext.Default.UpdateRoleRequest,
            ClientJsonContext.Default.ClientCommandResult, ct)).EnsureSuccess();
        return id;
    }
    public override async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        RequireId(id);
        (await Api.DeleteJsonAsync($"{Route}/{id:D}", ClientJsonContext.Default.ClientCommandResult, ct)).EnsureSuccess();
    }
    private static void RequireId(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Role ID is required.", nameof(id));
    }
}
