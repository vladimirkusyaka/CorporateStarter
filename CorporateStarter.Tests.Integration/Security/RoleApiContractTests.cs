using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using CorporateStarter.Api.Controllers.Security;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Security.Roles.Handlers;
using CorporateStarter.Application.Security.Roles.Models;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Roles;
using CorporateStarter.Tests.Integration.MasterData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CorporateStarter.Tests.Integration.Security;

// Actual MVC routing, model validation, authorization, exception filter and handlers;
// repositories are in-memory substitutes, so these tests do not need Docker.
public sealed class RoleApiContractTests : IDisposable
{
    private readonly Repository _repository = new();
    private readonly IHost _host;
    private readonly HttpClient _client;
    public RoleApiContractTests()
    {
        _host = new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllers().AddApplicationPart(typeof(RolesController).Assembly);
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<CreateRoleCommandHandler>());
            services.AddSingleton<IRoleReadRepository>(_repository);
            services.AddSingleton<IRoleWriteRepository>(_repository);
            services.AddSingleton<IRoleTableRepository>(_repository);
            services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("Test", _ => { });
            services.AddAuthorization(options =>
            {
                foreach (var p in AppPermissions.All) options.AddPolicy(p.Code, policy => policy.RequireClaim("permission", p.Code));
            });
        }).Configure(app => { app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(e => e.MapControllers()); })).Start();
        _client = _host.GetTestClient();
    }
    private void Permissions(params string[] values)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-Permissions");
        _client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(',', values));
    }
    [Fact]
    public async Task Role_update_preserves_permissions_without_manage_and_rejects_explicit_replacement()
    {
        Permissions(AppPermissions.RolesUpdate);
        var path = "/api/Roles/" + _repository.Role.Id;
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync(path, new UpdateRoleRequest { Name = "Renamed" })).StatusCode);
        Assert.Null(_repository.LastWrite!.PermissionIds);
        Assert.Equal(1, _repository.Writes);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PutAsJsonAsync(path, new UpdateRoleRequest { Name = "Renamed", PermissionIds = [] })).StatusCode);
        Assert.Equal(1, _repository.Writes);
        Permissions(AppPermissions.RolesUpdate, AppPermissions.RolesManagePermissions);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync(path, new UpdateRoleRequest { Name = "Renamed", PermissionIds = [] })).StatusCode);
        Assert.Empty(_repository.LastWrite!.PermissionIds!);
    }
    [Fact]
    public async Task Create_requires_manage_only_when_assigning_permissions()
    {
        Permissions(AppPermissions.RolesCreate);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/Roles", new CreateRoleRequest { Name = "Role" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Roles", new CreateRoleRequest { Name = "Role", PermissionIds = [Guid.NewGuid()] })).StatusCode);
        Permissions(AppPermissions.RolesCreate, AppPermissions.RolesManagePermissions);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/Roles", new CreateRoleRequest { Name = "Role", PermissionIds = [Guid.NewGuid()] })).StatusCode);
        Assert.Equal(2, _repository.Writes);
    }
    [Fact]
    public async Task System_roles_cannot_be_renamed_deactivated_or_deleted()
    {
        Permissions(AppPermissions.RolesUpdate, AppPermissions.RolesDelete, AppPermissions.RolesManagePermissions);
        _repository.Role.IsSystemRole = true;
        var path = "/api/Roles/" + _repository.Role.Id;
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PutAsJsonAsync(path, new UpdateRoleRequest { Name = "New", IsActive = false, PermissionIds = [] })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.DeleteAsync(path)).StatusCode);
        Assert.Equal(0, _repository.Writes);
    }
    [Fact]
    public async Task Table_and_capabilities_require_read_and_invalid_requests_become_400()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/Roles/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Roles/query", new TableRequest())).StatusCode);
        Permissions(AppPermissions.RolesRead);
        var caps = (await _client.GetFromJsonAsync<RoleCapabilities>("/api/Roles/capabilities"))!;
        Assert.False(caps.Table.CanUpdate); Assert.False(caps.CanManagePermissions);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Roles/query", new TableRequest { PageSize = 101 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/Roles/permission-options")).StatusCode);
    }
    [Fact]
    public async Task Invalid_lengths_are_rejected_before_write()
    {
        Permissions(AppPermissions.RolesCreate);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Roles", new CreateRoleRequest { Name = new string('x', 101) })).StatusCode);
        Assert.Equal(0, _repository.Writes);
    }
    public void Dispose() { _client.Dispose(); _host.Dispose(); }

    private sealed class Repository : IRoleReadRepository, IRoleWriteRepository, IRoleTableRepository
    {
        public RoleDetailsDto Role { get; } = new() { Id = Guid.NewGuid(), Name = "Role", IsActive = true };
        public RoleWriteValues? LastWrite { get; private set; }
        public int Writes { get; private set; }
        public Task<IReadOnlyList<RoleListItemDto>> GetListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<RoleListItemDto>>([]);
        public Task<RoleDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<RoleDetailsDto?>(id == Role.Id ? Role : null);
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(id == Role.Id);
        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> AllPermissionsExistAsync(IReadOnlyList<Guid> ids, CancellationToken ct) => Task.FromResult(true);
        public Task<bool> IsAdministratorRoleAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task<Guid> AddAsync(RoleWriteValues values, CancellationToken ct) { LastWrite = values; Writes++; return Task.FromResult(Guid.NewGuid()); }
        public Task UpdateAsync(RoleWriteValues values, CancellationToken ct) { LastWrite = values; Writes++; return Task.CompletedTask; }
        public Task DeactivateAsync(Guid id, CancellationToken ct) { Writes++; return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<PagedResult<RoleListItemDto>> QueryAsync(TableRequest request, CancellationToken ct)
        {
            if (request.PageSize > 100) throw new ArgumentException("Invalid table request.");
            return Task.FromResult(new PagedResult<RoleListItemDto> { Items = [], Page = 1, PageSize = request.PageSize });
        }
        public Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct) => Task.FromResult(new TableFindResult());
        public Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct) => Task.FromResult(new[] { "Active", "Inactive" });
    }
}
