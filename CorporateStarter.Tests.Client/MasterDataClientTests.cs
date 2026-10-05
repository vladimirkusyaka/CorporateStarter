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
    public async Task Query_rejects_duplicate_record_ids(string entity)
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new { items = new[] { new { id, name = "A", code = "AA" }, new { id, name = "B", code = "BB" } }, page = 1, pageSize = 20, totalCount = 2 });
        var api = CreateApi(new Transport { Response = new(200, json, "application/json") });
        if (entity == "Countries") await Assert.ThrowsAsync<InvalidDataException>(() => new CountriesClient(api).QueryAsync(new()));
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
