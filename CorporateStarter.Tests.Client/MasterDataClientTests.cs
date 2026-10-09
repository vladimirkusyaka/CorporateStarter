using CorporateStarter.Client.Core.Directory.Persons;
using CorporateStarter.Client.Core.Directory.Companies;
using CorporateStarter.Client.Core.Security.Users;
using CorporateStarter.Client.Core.Security.Roles;
using CorporateStarter.Shared.Dtos.Security.Roles;
using CorporateStarter.Client.Core.Security.Permissions;
using CorporateStarter.Client.Core.MasterData.Cities;
using System.Text.Json;
using CorporateStarter.Client.Abstractions.Api;
using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.Core.MasterData.Countries;
using CorporateStarter.Client.Core.MasterData.Positions;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.MasterData.Countries;
using CorporateStarter.Shared.Dtos.MasterData.Positions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CorporateStarter.Tests.Client;

public sealed class MasterDataClientTests
{
    [Theory]
    [InlineData("Countries")]
    [InlineData("Positions")]
    [InlineData("Cities")]
    [InlineData("Permissions")]
    [InlineData("Roles")]
    [InlineData("Users")]
    [InlineData("Companies")]
    [InlineData("Persons")]
    public async Task Query_preserves_filters_and_uses_entity_route(string entity)
    {
        var transport = new Transport { Response = new(200, "{\"items\":[],\"page\":2,\"pageSize\":10,\"totalCount\":10}", "application/json") };
        var api = CreateApi(transport);
        var request = new TableRequest
        {
            PageNumber = 2,
            PageSize = 10,
            Filters = [new("isActive", "in", Values: [JsonSerializer.SerializeToElement(false)])]
        };
        if (entity == "Countries") await new CountriesClient(api).QueryAsync(request);
        else if (entity == "Persons") await new PersonsClient(api).QueryAsync(request);
        else if (entity == "Companies") await new CompaniesClient(api).QueryAsync(request);
        else if (entity == "Users") await new UsersClient(api).QueryAsync(request);
        else if (entity == "Roles") await new RolesClient(api).QueryAsync(request);
        else if (entity == "Permissions") await new PermissionsClient(api).QueryAsync(request);
        else if (entity == "Cities") await new CitiesClient(api).QueryAsync(request);
        else await new PositionsClient(api).QueryAsync(request);
        Assert.Equal("/api/" + entity + "/query", transport.Path);
        Assert.Equal(HttpMethod.Post, transport.Method);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal(2, body.RootElement.GetProperty("pageNumber").GetInt32());
        Assert.False(body.RootElement.GetProperty("filters")[0].GetProperty("values")[0].GetBoolean());
    }

    [Theory]
    [InlineData("Countries")]
    [InlineData("Positions")]
    [InlineData("Cities")]
    [InlineData("Permissions")]
    [InlineData("Roles")]
    [InlineData("Users")]
    [InlineData("Companies")]
    [InlineData("Persons")]
    public async Task Query_rejects_duplicate_record_ids(string entity)
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new { items = new[] { new { id, name = "A", code = "AA" }, new { id, name = "B", code = "BB" } }, page = 1, pageSize = 20, totalCount = 2 });
        var api = CreateApi(new Transport { Response = new(200, json, "application/json") });
        if (entity == "Countries") await Assert.ThrowsAsync<InvalidDataException>(() => new CountriesClient(api).QueryAsync(new()));
        else if (entity == "Persons") await Assert.ThrowsAsync<InvalidDataException>(() => new PersonsClient(api).QueryAsync(new()));
        else if (entity == "Companies") await Assert.ThrowsAsync<InvalidDataException>(() => new CompaniesClient(api).QueryAsync(new()));
        else if (entity == "Users") await Assert.ThrowsAsync<InvalidDataException>(() => new UsersClient(api).QueryAsync(new()));
        else if (entity == "Roles") await Assert.ThrowsAsync<InvalidDataException>(() => new RolesClient(api).QueryAsync(new()));
        else if (entity == "Permissions") await Assert.ThrowsAsync<InvalidDataException>(() => new PermissionsClient(api).QueryAsync(new()));
        else if (entity == "Cities") await Assert.ThrowsAsync<InvalidDataException>(() => new CitiesClient(api).QueryAsync(new()));
        else await Assert.ThrowsAsync<InvalidDataException>(() => new PositionsClient(api).QueryAsync(new()));
    }

    [Fact]
    public async Task Position_update_sends_false_status_and_rejects_wrong_response_id()
    {
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new { id = Guid.NewGuid(), name = "Lead", isActive = false }), "application/json") };
        var client = new PositionsClient(CreateApi(transport));
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidDataException>(() => client.UpdateAsync(id, new() { Name = "Lead", IsActive = false }));
        Assert.Equal($"/api/Positions/{id:D}", transport.Path);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task Country_update_keeps_omitted_status_unchanged()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new { id, name = "Country", code = "AA", isActive = false }), "application/json") };
        await new CountriesClient(CreateApi(transport)).UpdateAsync(id, new() { Name = "Country", Code = "AA" });
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.False(body.RootElement.TryGetProperty("isActive", out _));
    }

    [Fact]
    public async Task Uncertain_create_is_not_replayed()
    {
        var transport = new Transport { Fail = true };
        await Assert.ThrowsAsync<ClientApiTransportException>(() => new PositionsClient(CreateApi(transport)).CreateAsync(new() { Name = "Lead" }));
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task Delete_requires_204_and_empty_id_never_reaches_transport()
    {
        var transport = new Transport { Response = new(200, "{}", "application/json") };
        var client = new PositionsClient(CreateApi(transport));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.DeleteAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentException>(() => client.DeleteAsync(Guid.Empty));
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task City_update_sends_country_region_and_inactive_status()
    {
        var id = Guid.NewGuid(); var countryId = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new { id, name = "Town", countryId, countryName = "Country", countryCode = "AA" }), "application/json") };
        await new CitiesClient(CreateApi(transport)).UpdateAsync(id, new() { Name = "Town", CountryId = countryId, Region = "North", IsActive = false });
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal(countryId, body.RootElement.GetProperty("countryId").GetGuid());
        Assert.Equal("North", body.RootElement.GetProperty("region").GetString());
        Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal($"/api/Cities/{id:D}", transport.Path);
    }

    [Fact]
    public async Task Country_lookup_is_bounded_and_rejects_duplicate_ids()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new[] { new LookupOption(id, "AA"), new LookupOption(id, "BB") }), "application/json") };
        await Assert.ThrowsAsync<InvalidDataException>(() => new CitiesClient(CreateApi(transport)).SearchCountriesAsync("Nor"));
        Assert.Equal("/api/Cities/country-options", transport.Path);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal("Nor", body.RootElement.GetProperty("text").GetString());
        Assert.Equal(20, body.RootElement.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task Permission_catalog_uses_capabilities_and_rejects_delete_without_HTTP()
    {
        var transport = new Transport
        {
            Response = new(200,
            "{\"canCreate\":false,\"canUpdate\":false,\"canDelete\":false,\"viewInactive\":true,\"canRestore\":false}", "application/json")
        };
        var client = new PermissionsClient(CreateApi(transport));
        Assert.Equal(new TableCapabilities(false, false, false, true, false), await client.GetCapabilitiesAsync());
        Assert.Equal("/api/Permissions/capabilities", transport.Path);
        Assert.Equal(HttpMethod.Get, transport.Method);
        await Assert.ThrowsAsync<NotSupportedException>(() => client.DeleteAsync(Guid.NewGuid()));
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task Role_commands_use_existing_envelope_and_omit_unchanged_permissions()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new { succeeded = true, value = id }), "application/json") };
        var client = new RolesClient(CreateApi(transport));
        Assert.Equal(id, await client.CreateAsync(new() { Name = "Auditor" }));
        Assert.Equal(id, await client.UpdateAsync(id, new() { Name = "Auditor", PermissionIds = null }));
        using (var body = JsonDocument.Parse(transport.Body!)) Assert.False(body.RootElement.TryGetProperty("permissionIds", out _));
        await client.UpdateAsync(id, new() { Name = "Auditor", PermissionIds = [] });
        using (var body = JsonDocument.Parse(transport.Body!)) Assert.Equal(0, body.RootElement.GetProperty("permissionIds").GetArrayLength());
        await client.DeleteAsync(id);
        Assert.Equal(HttpMethod.Delete, transport.Method);
        Assert.Equal($"/api/Roles/{id:D}", transport.Path);
        Assert.Equal(4, transport.Calls);
    }

    [Theory]
    [InlineData("{\"succeeded\":true}")]
    [InlineData("{\"succeeded\":false}")]
    [InlineData("{\"succeeded\":true,\"value\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task Role_create_rejects_unconfirmed_response_without_replay(string response)
    {
        var transport = new Transport { Response = new(200, response, "application/json") };
        await Assert.ThrowsAsync<InvalidDataException>(() => new RolesClient(CreateApi(transport)).CreateAsync(new() { Name = "Auditor" }));
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task User_update_preserves_roles_and_password_is_a_separate_request()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, "{\"succeeded\":true}", "application/json") };
        var client = new UsersClient(CreateApi(transport));
        await client.UpdateAsync(id, new() { Login = "tester", Email = "tester@example.test", IsActive = false });
        using (var body = JsonDocument.Parse(transport.Body!))
        {
            Assert.False(body.RootElement.TryGetProperty("roleIds", out _));
            Assert.False(body.RootElement.TryGetProperty("password", out _));
            Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
        }
        await client.UpdateAsync(id, new() { RoleIds = [] });
        using (var body = JsonDocument.Parse(transport.Body!)) Assert.Equal(0, body.RootElement.GetProperty("roleIds").GetArrayLength());
        await client.ChangePasswordAsync(id, new() { NewPassword = "Test-only-secret" });
        Assert.Equal($"/api/Users/{id:D}/password", transport.Path);
        Assert.Equal(HttpMethod.Put, transport.Method);
        Assert.Equal(3, transport.Calls);
    }

    [Fact]
    public async Task User_validation_keeps_error_code_without_leaking_raw_response()
    {
        var transport = new Transport { Response = new(400, "{\"errorCode\":\"user.password_reused\",\"errorMessage\":\"private detail\"}", "application/json") };
        var exception = await Assert.ThrowsAsync<ClientApiHttpException>(() => new UsersClient(CreateApi(transport)).ChangePasswordAsync(Guid.NewGuid(), new()));
        Assert.Equal("user.password_reused", exception.ErrorCode);
        Assert.DoesNotContain("private detail", exception.Message);
        Assert.NotNull(CommandErrorMessages.ForCode(exception.ErrorCode));
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task User_save_with_lost_response_is_not_replayed()
    {
        var transport = new Transport { Fail = true };
        await Assert.ThrowsAsync<ClientApiTransportException>(() => new UsersClient(CreateApi(transport)).CreateAsync(new()));
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task Company_update_clears_optional_city_preserves_contacts_and_uses_details_contract()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new { id, name = "Example" }), "application/json") };
        var client = new CompaniesClient(CreateApi(transport));
        await client.UpdateAsync(id, new() { Name = "Example", CityId = null, Email = "office@example.test", Street = "Main", IsActive = false });
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("cityId").ValueKind);
        Assert.Equal("office@example.test", body.RootElement.GetProperty("email").GetString());
        Assert.Equal("Main", body.RootElement.GetProperty("street").GetString());
        Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal($"/api/Companies/{id:D}", transport.Path);
        Assert.Equal(1, transport.Calls);
    }

    [Fact]
    public async Task Company_lookup_is_bounded_and_company_write_is_not_replayed()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new[] { new LookupOption(id, "City"), new LookupOption(id, "City") }), "application/json") };
        await Assert.ThrowsAsync<InvalidDataException>(() => new CompaniesClient(CreateApi(transport)).SearchCitiesAsync("York"));
        Assert.Equal("/api/Companies/city-options", transport.Path);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal(20, body.RootElement.GetProperty("limit").GetInt32());
        var lost = new Transport { Fail = true };
        await Assert.ThrowsAsync<ClientApiTransportException>(() => new CompaniesClient(CreateApi(lost)).CreateAsync(new() { Name = "Example" }));
        Assert.Equal(1, lost.Calls);
    }

    [Fact]
    public async Task Person_date_is_calendar_date_and_optional_links_can_be_cleared()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new { id, firstName = "Alex", dateOfBirth = "1990-02-03" }), "application/json") };
        var client = new PersonsClient(CreateApi(transport));
        var result = await client.UpdateAsync(id, new() { FirstName = "Alex", DateOfBirth = new(1990, 2, 3), CompanyId = null, PositionId = null, IsActive = false });
        Assert.Equal(new DateOnly(1990, 2, 3), result.DateOfBirth);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal("1990-02-03", body.RootElement.GetProperty("dateOfBirth").GetString());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("companyId").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("positionId").ValueKind);
        Assert.False(body.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal($"/api/Persons/{id:D}", transport.Path);
    }
    [Fact]
    public async Task Person_options_use_separate_routes_and_unknown_write_is_not_replayed()
    {
        var transport = new Transport { Response = new(200, "[]", "application/json") };
        var client = new PersonsClient(CreateApi(transport));
        await client.SearchCompaniesAsync("Example"); Assert.Equal("/api/Persons/company-options", transport.Path);
        await client.SearchPositionsAsync("Manager"); Assert.Equal("/api/Persons/position-options", transport.Path);
        var lost = new Transport { Fail = true };
        await Assert.ThrowsAsync<ClientApiTransportException>(() => new PersonsClient(CreateApi(lost)).CreateAsync(new() { FirstName = "Alex" }));
        Assert.Equal(1, lost.Calls);
    }

    [Fact]
    public async Task Changes_client_preserves_utc_and_never_sends_delete()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new { id, entityName = "Person", createdAtUtc = "2026-10-09T12:30:00Z", oldValuesJson = "{}", newValuesJson = "{}" }), "application/json") };
        var client = new CorporateStarter.Client.Core.Audit.ChangesClient(CreateApi(transport));
        var detail = await client.GetDetailsAsync(id);
        Assert.Equal(DateTimeKind.Utc, detail.CreatedAtUtc.Kind);
        Assert.Equal($"/api/Audit/{id:D}", transport.Path);
        await Assert.ThrowsAsync<NotSupportedException>(() => client.DeleteAsync(id));
        Assert.Equal(1, transport.Calls);
        await Assert.ThrowsAsync<InvalidDataException>(() => client.GetDetailsAsync(Guid.NewGuid()));
    }
    [Fact]
    public async Task Changes_query_uses_shared_table_contract()
    {
        var id = Guid.NewGuid();
        var transport = new Transport { Response = new(200, JsonSerializer.Serialize(new { items = new[] { new { id, entityName = "Person", action = "Update" } }, page = 1, pageSize = 20, totalCount = 1 }), "application/json") };
        var client = new CorporateStarter.Client.Core.Audit.ChangesClient(CreateApi(transport));
        var page = await client.QueryAsync(new() { PageSize = 20 });
        Assert.Equal(id, Assert.Single(page.Items).Id);
        Assert.Equal("/api/Audit/query", transport.Path);
    }

    private static ClientApiClient CreateApi(Transport transport)
    {
        var state = new ClientAuthStateStore(NullLogger<ClientAuthStateStore>.Instance);
        state.TryTransition(0, ClientAuthStatus.Authenticated, transport.UserId);
        return new(transport, state, new ClientSessionCoordinator(state, transport, transport, transport));
    }

    private sealed class Transport : IClientApiTransport, IClientSessionTransport, IClientLoginTransport, IClientLogoutTransport
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public ClientApiResponse Response { get; init; } = new(204, "");
        public bool Fail { get; init; }
        public int Calls { get; private set; }
        public string? Path { get; private set; }
        public string? Body { get; private set; }
        public HttpMethod? Method { get; private set; }
        public Task<ClientApiResponse> SendAsync(HttpMethod method, string relativePath, Guid expectedUserId, string? jsonBody = null, CancellationToken cancellationToken = default)
        {
            Assert.Equal(UserId, expectedUserId);
            Calls++; Path = relativePath; Body = jsonBody; Method = method;
            if (Fail) throw new ClientApiTransportException("Lost response.");
            return Task.FromResult(Response);
        }
        public Task<ClientSessionRestoreResult> RestoreAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ClientSessionRestoreResult.Authenticated(UserId));
        public Task<ClientLoginResult> LoginAsync(string login, string password) => throw new NotSupportedException();
        public Task<ClientLogoutStatus> LogoutAsync() => throw new NotSupportedException();
    }
}
