using System.Net;
using System.Net.Http.Json;
using CorporateStarter.Api.Controllers.Directory;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Cities;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Directory.Companies.Handlers;
using CorporateStarter.Application.Directory.Companies.Models;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Companies;
using CorporateStarter.Shared.Dtos.MasterData.Cities;
using CorporateStarter.Tests.Integration.MasterData;
using Mapster;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
namespace CorporateStarter.Tests.Integration.Directory;

// MVC, authorization and the real command handlers; repositories are test doubles.
public sealed class CompanyApiContractTests : IDisposable
{
    private readonly Repository _repository = new();
    private readonly IHost _host;
    private readonly HttpClient _client;
    public CompanyApiContractTests()
    {
        _host = new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllers().AddApplicationPart(typeof(CompaniesController).Assembly);
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<CreateCompanyCommandHandler>());
            services.AddSingleton<ICompanyReadRepository>(_repository);
            services.AddSingleton<ICompanyWriteRepository>(_repository);
            services.AddSingleton<ICompanyTableRepository>(_repository);
            services.AddSingleton<ICityReadRepository>(_repository);
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
    public async Task Read_and_write_policies_are_independent_and_bad_queries_return_400()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/Companies/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Companies/query", new TableRequest())).StatusCode);
        Permissions(AppPermissions.CompaniesRead);
        var caps = (await _client.GetFromJsonAsync<TableCapabilities>("/api/Companies/capabilities"))!;
        Assert.False(caps.CanCreate); Assert.False(caps.CanUpdate); Assert.True(caps.ViewInactive);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/Companies/query", new TableRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Companies/query", new TableRequest { PageSize = 101 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Companies", new CreateCompanyRequest { Name = "Example" })).StatusCode);
    }
    [Fact]
    public async Task City_lookup_requires_company_edit_rights_without_cities_read()
    {
        Permissions(AppPermissions.CompaniesRead);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Companies/city-options", new LookupRequest())).StatusCode);
        Permissions(AppPermissions.CompaniesRead, AppPermissions.CompaniesCreate);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/Companies/city-options", new LookupRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Companies/city-options", new LookupRequest { Limit = 51 })).StatusCode);
    }
    [Fact]
    public async Task Create_update_and_delete_keep_existing_response_and_optional_city_contract()
    {
        Permissions(AppPermissions.CompaniesCreate, AppPermissions.CompaniesUpdate, AppPermissions.CompaniesDelete);
        var response = await _client.PostAsJsonAsync("/api/Companies", new CreateCompanyRequest { Name = " Example ", Email = "office@example.test", CityId = null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await response.Content.ReadFromJsonAsync<CompanyDetailsDto>())!;
        Assert.True(item.IsActive); Assert.Equal("Example", item.Name); Assert.Null(item.CityId);
        Assert.NotNull(response.Headers.Location);
        var city = Guid.NewGuid();
        response = await _client.PutAsJsonAsync("/api/Companies/" + item.Id, new UpdateCompanyRequest { Name = "Example", CityId = city, IsActive = false, Street = "Main", LegalName = "Example Ltd" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        item = (await response.Content.ReadFromJsonAsync<CompanyDetailsDto>())!;
        Assert.Equal(city, item.CityId); Assert.False(item.IsActive); Assert.Equal("Main", item.Street);
        response = await _client.PutAsJsonAsync("/api/Companies/" + item.Id, new UpdateCompanyRequest { Name = "Example", CityId = null, IsActive = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await response.Content.ReadFromJsonAsync<CompanyDetailsDto>())!.CityId);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync("/api/Companies/" + item.Id)).StatusCode);
        Assert.False(_repository.Item!.IsActive);
    }
    [Fact]
    public async Task Duplicate_missing_city_and_invalid_lengths_do_not_write()
    {
        Permissions(AppPermissions.CompaniesCreate);
        _repository.Duplicate = true;
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/Companies", new CreateCompanyRequest { Name = "Example" })).StatusCode);
        _repository.Duplicate = false; _repository.MissingCity = true;
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/Companies", new CreateCompanyRequest { Name = "Example", CityId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Companies", new CreateCompanyRequest { Name = "Example", PostalCode = new string('x', 21) })).StatusCode);
        Assert.Equal(0, _repository.Writes);
    }
    public void Dispose() { _client.Dispose(); _host.Dispose(); }

    private sealed class Repository : ICompanyReadRepository, ICompanyWriteRepository, ICompanyTableRepository, ICityReadRepository
    {
        public CompanyDetailsDto? Item;
        public bool Duplicate, MissingCity;
        public int Writes;
        public Task<IReadOnlyList<CompanyListItemDto>> GetListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<CompanyListItemDto>>([]);
        public Task<CompanyDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Item?.Id == id ? Item : null);
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Item?.Id == id);
        public Task<bool> ExistsByCodeAsync(string code, Guid? id, CancellationToken ct) => Task.FromResult(Duplicate);
        public Task<bool> ExistsByNameAsync(string name, Guid? id, CancellationToken ct) => Task.FromResult(Duplicate);
        public Task<Company> AddAsync(CompanyWriteValues values, CancellationToken ct)
        {
            Item = values.Adapt<CompanyDetailsDto>(); Item.Id = Guid.NewGuid(); Writes++;
            return Task.FromResult(new Company { Id = Item.Id, Name = Item.Name });
        }
        public Task<bool> UpdateAsync(CompanyWriteValues values, CancellationToken ct)
        {
            if (Item?.Id != values.Id) return Task.FromResult(false);
            Item = values.Adapt<CompanyDetailsDto>(); Writes++; return Task.FromResult(true);
        }
        public Task<bool> DeactivateAsync(Guid id, CancellationToken ct)
        { if (Item?.Id != id) return Task.FromResult(false); Item.IsActive = false; Writes++; return Task.FromResult(true); }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
        Task<IReadOnlyList<CityListItemDto>> ICityReadRepository.GetListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<CityListItemDto>>([]);
        Task<CityDetailsDto?> ICityReadRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<CityDetailsDto?>(null);
        Task<bool> ICityReadRepository.ExistsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(!MissingCity);
        public Task<bool> ExistsByNameAsync(Guid country, string name, string? region, Guid? id, CancellationToken ct) => Task.FromResult(false);
        public Task<PagedResult<CompanyListItemDto>> QueryAsync(TableRequest request, CancellationToken ct)
        {
            if (request.PageSize > 100) throw new ArgumentException("Invalid table request.");
            return Task.FromResult(new PagedResult<CompanyListItemDto> { Items = [], Page = 1, PageSize = request.PageSize });
        }
        public Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct) => Task.FromResult(new TableFindResult());
        public Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct) => Task.FromResult(new[] { "Active", "Inactive" });
        public Task<LookupOption[]> SearchCitiesAsync(LookupRequest request, CancellationToken ct)
        {
            if (request.Limit > 50) throw new ArgumentException("Invalid lookup.");
            return Task.FromResult(Array.Empty<LookupOption>());
        }
    }
}
