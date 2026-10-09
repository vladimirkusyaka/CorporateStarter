using System.Net;
using System.Net.Http.Json;
using CorporateStarter.Api.Controllers.Audit;
using CorporateStarter.Application.Common.Interfaces.Repositories.Audit;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Audit;
using CorporateStarter.Tests.Integration.MasterData;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
namespace CorporateStarter.Tests.Integration.Audit;

public sealed class ChangesApiTests
{
    [Fact]
    public async Task Changes_require_audit_read_are_paginated_and_have_no_write_routes()
    {
        var repo = new Repository();
        using var host = new HostBuilder().ConfigureWebHost(b => b.UseTestServer().ConfigureServices(s =>
        {
            s.AddLogging(); s.AddControllers().AddApplicationPart(typeof(AuditController).Assembly);
            s.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<CorporateStarter.Application.Audit.Handlers.GetAuditsQueryHandler>());
            s.AddSingleton<IChangeTableRepository>(repo);
            s.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TableTestAuthentication>("Test", _ => { });
            s.AddAuthorization(o => o.AddPolicy(AppPermissions.AuditRead, p => p.RequireClaim("permission", AppPermissions.AuditRead)));
        }).Configure(a => { a.UseRouting(); a.UseAuthentication(); a.UseAuthorization(); a.UseEndpoints(e => e.MapControllers()); })).Start();
        using var client = host.GetTestClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Audit/query", new TableRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Audit/" + repo.Id)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Permissions", AppPermissions.AuditRead);
        var caps = (await client.GetFromJsonAsync<TableCapabilities>("/api/Audit/capabilities"))!;
        Assert.False(caps.CanCreate); Assert.False(caps.CanUpdate); Assert.False(caps.CanDelete);
        var response = await client.PostAsJsonAsync("/api/Audit/query", new TableRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("oldValuesJson", await response.Content.ReadAsStringAsync());
        var detail = (await client.GetFromJsonAsync<AuditLogListItemDto>("/api/Audit/" + repo.Id))!;
        Assert.Equal("{}", detail.NewValuesJson);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/Audit/" + Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/Audit/query", new TableRequest { PageSize = 101 })).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.DeleteAsync("/api/Audit/" + repo.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/Audit", new { })).StatusCode);
    }
    private sealed class Repository : IChangeTableRepository
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Task<PagedResult<ChangeListItemDto>> QueryAsync(TableRequest r, CancellationToken ct)
        {
            if (r.PageSize > 100) throw new ArgumentException("Invalid request.");
            return Task.FromResult(new PagedResult<ChangeListItemDto> { Items = [new() { Id = Id, EntityName = "Person", Action = "Create" }], Page = 1, PageSize = r.PageSize, TotalCount = 1 });
        }
        public Task<AuditLogListItemDto?> GetDetailsAsync(Guid id, CancellationToken ct) => Task.FromResult<AuditLogListItemDto?>(id == Id ? new() { Id = Id, EntityName = "Person", NewValuesJson = "{}" } : null);
        public Task<TableFindResult> FindAsync(TableFindRequest r, CancellationToken ct) => Task.FromResult(new TableFindResult());
        public Task<string[]> ValuesAsync(TableValuesRequest r, CancellationToken ct) => Task.FromResult(Array.Empty<string>());
    }
}
