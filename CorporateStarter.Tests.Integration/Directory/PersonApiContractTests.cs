using CorporateStarter.Application.Common.Interfaces.Repositories.MasterData.Positions;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using System.Net;
using System.Net.Http.Json;
using CorporateStarter.Api.Controllers.Directory;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Persons;
using CorporateStarter.Application.Common.Interfaces.Repositories.Directory.Companies;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Application.Directory.Persons.Handlers;
using CorporateStarter.Application.Directory.Persons.Models;
using CorporateStarter.Core.Entities.Directory;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Directory.Persons;
using CorporateStarter.Shared.Dtos.Directory.Companies;
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
public sealed class PersonApiContractTests : IDisposable
{
    private readonly Repository _repository = new();
    private readonly IHost _host;
    private readonly HttpClient _client;
    public PersonApiContractTests()
    {
        _host = new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllers().AddApplicationPart(typeof(PersonsController).Assembly);
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<CreatePersonCommandHandler>());
            services.AddSingleton<IPersonReadRepository>(_repository);
            services.AddSingleton<IPersonWriteRepository>(_repository);
            services.AddSingleton<IPersonTableRepository>(_repository);
            services.AddSingleton<ICompanyReadRepository>(_repository);
            services.AddSingleton<IPositionReadRepository>(_repository);
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
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/Persons/capabilities")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Persons/query", new TableRequest())).StatusCode);
        Permissions(AppPermissions.PersonsRead);
        var caps = (await _client.GetFromJsonAsync<TableCapabilities>("/api/Persons/capabilities"))!;
        Assert.False(caps.CanCreate); Assert.False(caps.CanUpdate); Assert.True(caps.ViewInactive);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/Persons/query", new TableRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Persons/query", new TableRequest { PageSize = 101 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Persons", new CreatePersonRequest { FirstName = "Example" })).StatusCode);
    }
    [Theory]
    [InlineData("company-options")]
    [InlineData("position-options")]
    public async Task Lookup_requires_person_edit_rights_without_reference_read(string route)
    {
        Permissions(AppPermissions.PersonsRead);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/Persons/" + route, new LookupRequest())).StatusCode);
        Permissions(AppPermissions.PersonsRead, AppPermissions.PersonsCreate);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/Persons/" + route, new LookupRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Persons/" + route, new LookupRequest { Limit = 51 })).StatusCode);
    }
    [Fact]
    public async Task Create_update_and_delete_keep_existing_response_and_optional_links_contract()
    {
        Permissions(AppPermissions.PersonsCreate, AppPermissions.PersonsUpdate, AppPermissions.PersonsDelete);
        var response = await _client.PostAsJsonAsync("/api/Persons", new CreatePersonRequest { FirstName = " Example ", Email = "office@example.test", CompanyId = null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = (await response.Content.ReadFromJsonAsync<PersonDetailsDto>())!;
        Assert.True(item.IsActive); Assert.Equal("Example", item.FirstName); Assert.Null(item.CompanyId);
        Assert.NotNull(response.Headers.Location);
        var company = Guid.NewGuid(); var position = Guid.NewGuid();
        response = await _client.PutAsJsonAsync("/api/Persons/" + item.Id, new UpdatePersonRequest { FirstName = "Example", CompanyId = company, PositionId = position, IsActive = false, Description = "Main", DateOfBirth = new(1990, 2, 3) });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        item = (await response.Content.ReadFromJsonAsync<PersonDetailsDto>())!;
        Assert.Equal(company, item.CompanyId); Assert.Equal(position, item.PositionId); Assert.Equal(new DateOnly(1990, 2, 3), item.DateOfBirth); Assert.False(item.IsActive); Assert.Equal("Main", item.Description);
        response = await _client.PutAsJsonAsync("/api/Persons/" + item.Id, new UpdatePersonRequest { FirstName = "Example", CompanyId = null, IsActive = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cleared = (await response.Content.ReadFromJsonAsync<PersonDetailsDto>())!; Assert.Null(cleared.CompanyId); Assert.Null(cleared.PositionId);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync("/api/Persons/" + item.Id)).StatusCode);
        Assert.False(_repository.Item!.IsActive);
    }
    [Fact]
    public async Task Missing_references_and_invalid_lengths_do_not_write()
    {
        Permissions(AppPermissions.PersonsCreate);
        _repository.MissingCompany = true;
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/Persons", new CreatePersonRequest { FirstName = "Alex", CompanyId = Guid.NewGuid() })).StatusCode);
        _repository.MissingCompany = false; _repository.MissingPosition = true;
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync("/api/Persons", new CreatePersonRequest { FirstName = "Alex", PositionId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/Persons", new CreatePersonRequest { FirstName = new string('x', 101) })).StatusCode);
        Assert.Equal(0, _repository.Writes);
    }
    public void Dispose() { _client.Dispose(); _host.Dispose(); }

    private sealed class Repository : IPersonReadRepository, IPersonWriteRepository, IPersonTableRepository, ICompanyReadRepository, IPositionReadRepository
    {
        public PersonDetailsDto? Item;
        public bool MissingCompany, MissingPosition;
        public int Writes;
        public Task<IReadOnlyList<PersonListItemDto>> GetListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<PersonListItemDto>>([]);
        public Task<PersonDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Item?.Id == id ? Item : null);
        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Item?.Id == id);
        public Task<bool> ExistsByCodeAsync(string code, Guid? id, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> ExistsByNameAsync(string name, Guid? id, CancellationToken ct) => Task.FromResult(false);
        public Task<Person> AddAsync(PersonWriteValues values, CancellationToken ct)
        {
            Item = values.Adapt<PersonDetailsDto>(); Item.Id = Guid.NewGuid(); Writes++;
            return Task.FromResult(new Person { Id = Item.Id, FirstName = Item.FirstName });
        }
        public Task<bool> UpdateAsync(PersonWriteValues values, CancellationToken ct)
        {
            if (Item?.Id != values.Id) return Task.FromResult(false);
            Item = values.Adapt<PersonDetailsDto>(); Writes++; return Task.FromResult(true);
        }
        public Task<bool> DeactivateAsync(Guid id, CancellationToken ct)
        { if (Item?.Id != id) return Task.FromResult(false); Item.IsActive = false; Writes++; return Task.FromResult(true); }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
        Task<IReadOnlyList<CompanyListItemDto>> ICompanyReadRepository.GetListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<CompanyListItemDto>>([]);
        Task<CompanyDetailsDto?> ICompanyReadRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<CompanyDetailsDto?>(null);
        Task<bool> ICompanyReadRepository.ExistsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(!MissingCompany);
        public Task<PagedResult<PersonListItemDto>> QueryAsync(TableRequest request, CancellationToken ct)
        {
            if (request.PageSize > 100) throw new ArgumentException("Invalid table request.");
            return Task.FromResult(new PagedResult<PersonListItemDto> { Items = [], Page = 1, PageSize = request.PageSize });
        }
        public Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct) => Task.FromResult(new TableFindResult());
        public Task<string[]> ValuesAsync(TableValuesRequest request, CancellationToken ct) => Task.FromResult(new[] { "Active", "Inactive" });
        public Task<LookupOption[]> SearchCompaniesAsync(LookupRequest request, CancellationToken ct)
        {
            if (request.Limit > 50) throw new ArgumentException("Invalid lookup.");
            return Task.FromResult(Array.Empty<LookupOption>());
        }
        Task<IReadOnlyList<PositionListItemDto>> IPositionReadRepository.GetListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<PositionListItemDto>>([]);
        Task<PositionDetailsDto?> IPositionReadRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<PositionDetailsDto?>(null);
        Task<bool> IPositionReadRepository.ExistsByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(!MissingPosition);
        public Task<LookupOption[]> SearchPositionsAsync(LookupRequest request, CancellationToken ct) => SearchCompaniesAsync(request, ct);
    }
}
