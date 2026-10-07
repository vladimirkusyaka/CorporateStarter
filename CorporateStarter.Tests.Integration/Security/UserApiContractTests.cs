using System.Net;
using System.Net.Http.Json;
using CorporateStarter.Api.Controllers.Security;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Security.Users.Handlers;
using CorporateStarter.Application.Security.Users.Models;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Infrastructure.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Security.Users;
using CorporateStarter.Shared.Dtos.Security.Roles;
using CorporateStarter.Tests.Integration.MasterData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CorporateStarter.Tests.Integration.Security;

public sealed class UserApiContractTests : IDisposable
{
    private readonly Repository _repository = new();
    private readonly IHost _host;
    private readonly HttpClient _client;
    public UserApiContractTests()
    {
        _host = new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllers().AddApplicationPart(typeof(UsersController).Assembly);
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<UpdateUserCommandHandler>());
            services.AddSingleton<IUserReadRepository>(_repository);
            services.AddSingleton<IUserWriteRepository>(_repository);
            services.AddSingleton<IUserTableRepository>(_repository);
            services.AddSingleton<IRoleReadRepository>(_repository);
            services.AddSingleton<ICurrentUserService>(_repository);
            services.AddSingleton<IPasswordHistoryRepository>(_repository);
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
            services.AddSingleton(new PasswordPolicyOptions());
            services.AddSingleton<IPasswordPolicyValidator, PasswordPolicyValidator>();
            services.AddSingleton(new PasswordHistoryOptions());
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
    private string Path => "/api/Users/" + _repository.User.Id;
    private static UpdateUserRequest Update(bool active = true, IReadOnlyList<Guid>? roles = null) =>
        new() { Login = "tester", Email = "tester@example.test", IsActive = active, RoleIds = roles };

    [Fact]
    public async Task Table_routes_require_read_and_invalid_query_returns_400()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/Users/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Users/query", new TableRequest())).StatusCode);
        Permissions(AppPermissions.UsersRead);
        var caps = (await _client.GetFromJsonAsync<UserCapabilities>("/api/Users/capabilities"))!;
        Assert.False(caps.Table.CanUpdate); Assert.False(caps.CanChangePassword);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/Users/query", new TableRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Users/query", new TableRequest { PageSize = 101 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/Users/role-options")).StatusCode);
        Permissions(AppPermissions.UsersRead, AppPermissions.UsersUpdate);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/Users/role-options")).StatusCode);
    }
    [Fact]
    public async Task Null_roles_preserve_assignments_and_invalid_replacements_do_not_write()
    {
        Permissions(AppPermissions.UsersUpdate);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync(Path, Update())).StatusCode);
        Assert.False(_repository.LastWrite!.ReplaceRoles);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(Path, Update(roles: [Guid.NewGuid()]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(Path, Update(roles: []))).StatusCode);
        Assert.Equal(1, _repository.Writes);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync(Path, Update(false, []))).StatusCode);
        Assert.True(_repository.LastWrite.ReplaceRoles); Assert.Empty(_repository.LastWrite.RoleIds);
    }
    [Fact]
    public async Task Inactive_existing_roles_are_preserved_but_cannot_be_newly_assigned()
    {
        Permissions(AppPermissions.UsersUpdate);
        var inactive = new RoleListItemDto { Id = Guid.NewGuid(), Name = "Retired", IsActive = false };
        _repository.Roles.Add(inactive);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(Path, Update(roles: [_repository.ActiveRole.Id, inactive.Id]))).StatusCode);
        _repository.User.Roles = [new() { RoleId = _repository.ActiveRole.Id }, new() { RoleId = inactive.Id }];
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync(Path, Update(roles: [_repository.ActiveRole.Id, inactive.Id]))).StatusCode);
        Assert.Contains(inactive.Id, _repository.LastWrite!.RoleIds);
    }
    [Fact]
    public async Task Self_and_last_administrator_protection_applies_to_update_and_delete()
    {
        Permissions(AppPermissions.UsersUpdate, AppPermissions.UsersDelete);
        _repository.UserId = _repository.User.Id;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(Path, Update(false))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync(Path)).StatusCode);
        _repository.UserId = Guid.NewGuid();
        _repository.IsAdmin = true;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(Path, Update())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync(Path)).StatusCode);
        Assert.Equal(0, _repository.Writes);
        _repository.OtherAdmin = true;
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync(Path)).StatusCode);
    }
    [Fact]
    public async Task Password_permission_is_separate_and_policy_is_enforced()
    {
        Permissions(AppPermissions.UsersUpdate);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PutAsJsonAsync(Path + "/password", new ChangeUserPasswordRequest { NewPassword = "Different!2468Ab" })).StatusCode);
        Permissions(AppPermissions.UsersChangePassword);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(Path + "/password", new ChangeUserPasswordRequest { NewPassword = "weak" })).StatusCode);
        Assert.Equal(0, _repository.Writes);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync(Path + "/password", new ChangeUserPasswordRequest { NewPassword = "Different!2468Ab" })).StatusCode);
        Assert.Equal(1, _repository.Writes);
    }
    [Fact]
    public async Task Duplicate_login_and_invalid_lengths_do_not_write()
    {
        Permissions(AppPermissions.UsersUpdate);
        _repository.DuplicateLogin = true;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PutAsJsonAsync(Path, Update())).StatusCode);
        var invalid = Update(); invalid.DisplayName = new string('x', 257);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync(Path, invalid)).StatusCode);
        Assert.Equal(0, _repository.Writes);
    }
    public void Dispose() { _client.Dispose(); _host.Dispose(); }

    private sealed class Repository : IUserReadRepository, IUserWriteRepository, IUserTableRepository,
        IRoleReadRepository, ICurrentUserService, IPasswordHistoryRepository
    {
        public UserDetailsDto User { get; } = new() { Id = Guid.NewGuid(), Login = "tester", Email = "tester@example.test", IsActive = true };
        public RoleListItemDto ActiveRole { get; } = new() { Id = Guid.NewGuid(), Name = "Reader", IsActive = true };
        public List<RoleListItemDto> Roles { get; } = [];
        public Repository() { Roles.Add(ActiveRole); User.Roles = [new() { RoleId = ActiveRole.Id, Name = ActiveRole.Name }]; }
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public string? Login => "operator";
        public string? Email => "operator@example.test";
        public bool IsAuthenticated => true;
        public bool IsAdmin, OtherAdmin, DuplicateLogin;
        public int Writes;
        public UserWriteValues? LastWrite;
        public Task<IReadOnlyList<UserListItemDto>> GetListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<UserListItemDto>>([]);
        Task<IReadOnlyList<RoleListItemDto>> IRoleReadRepository.GetListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<RoleListItemDto>>(Roles);
        public Task<UserDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<UserDetailsDto?>(id == User.Id ? User : null);
        Task<RoleDetailsDto?> IRoleReadRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<RoleDetailsDto?>(null);
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(id == User.Id);
        public Task<bool> ExistsByLoginAsync(string value, Guid? id, CancellationToken ct) => Task.FromResult(DuplicateLogin);
        public Task<bool> ExistsByEmailAsync(string value, Guid? id, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> ExistsByNameAsync(string value, Guid? id, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> AllRolesExistAsync(IReadOnlyList<Guid> ids, CancellationToken ct) => Task.FromResult(ids.All(id => Roles.Any(x => x.Id == id && x.IsActive)));
        public Task<bool> AllPermissionsExistAsync(IReadOnlyList<Guid> ids, CancellationToken ct) => Task.FromResult(true);
        public Task<bool> IsAdministratorRoleAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> IsActiveAdministratorAsync(Guid id, CancellationToken ct) => Task.FromResult(IsAdmin);
        public Task<bool> HasOtherActiveAdministratorAsync(Guid id, CancellationToken ct) => Task.FromResult(OtherAdmin);
        public Task<Guid> AddAsync(UserWriteValues values, string hash, CancellationToken ct) { LastWrite = values; Writes++; return Task.FromResult(Guid.NewGuid()); }
        public Task UpdateAsync(UserWriteValues values, CancellationToken ct) { LastWrite = values; Writes++; return Task.CompletedTask; }
        public Task ChangePasswordAsync(Guid id, string hash, CancellationToken ct) { Writes++; return Task.CompletedTask; }
        public Task DeactivateAsync(Guid id, CancellationToken ct) { Writes++; return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<string>> GetRecentPasswordHashesAsync(Guid id, int count, CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task AddAsync(PasswordHistory history, CancellationToken ct) => Task.CompletedTask;
        public Task<PagedResult<UserTableItemDto>> QueryAsync(TableRequest request, CancellationToken ct)
        {
            if (request.PageSize > 100) throw new ArgumentException("Invalid table request.");
            return Task.FromResult(new PagedResult<UserTableItemDto> { Items = [], Page = 1, PageSize = request.PageSize });
        }
        public Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct) => Task.FromResult(new TableFindResult());
        public Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct) => Task.FromResult(new[] { "Active", "Inactive" });
    }
}
