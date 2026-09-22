using CorporateStarter.Client.Core.Api;
using CorporateStarter.Shared.Common;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Shared.Dtos.MasterData.Countries;

namespace CorporateStarter.Client.Core.MasterData.Countries;

public sealed class CountriesClient
{
    private readonly ClientApiClient _api;

    public CountriesClient(ClientApiClient api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    public async Task<CountryDetailsDto> CreateAsync(
        CreateCountryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var country = await _api.PostJsonAsync(
            "/api/Countries", request, ClientJsonContext.Default.CreateCountryRequest,
            ClientJsonContext.Default.CountryDetailsDto, cancellationToken).ConfigureAwait(false);
        if (country.Id == Guid.Empty || string.IsNullOrWhiteSpace(country.Code) ||
            string.IsNullOrWhiteSpace(country.Name))
            throw new InvalidDataException("The API returned an invalid created country.");
        return country;
    }

    public async Task<CountryDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Country ID is required.", nameof(id));
        var country = await _api.GetJsonAsync($"/api/Countries/{id:D}",
            ClientJsonContext.Default.CountryDetailsDto, cancellationToken).ConfigureAwait(false);
        ValidateDetails(country, id);
        return country;
    }

    public async Task<CountryDetailsDto> UpdateAsync(Guid id, UpdateCountryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Country ID is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(request);
        var country = await _api.PutJsonAsync($"/api/Countries/{id:D}", request,
            ClientJsonContext.Default.UpdateCountryRequest, ClientJsonContext.Default.CountryDetailsDto,
            cancellationToken).ConfigureAwait(false);
        ValidateDetails(country, id);
        return country;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Country ID is required.", nameof(id));
        return _api.DeleteAsync($"/api/Countries/{id:D}", cancellationToken);
    }

    public Task<CountryCapabilities> CapabilitiesAsync(CancellationToken ct = default) =>
        _api.GetJsonAsync("/api/Countries/capabilities", ClientJsonContext.Default.CountryCapabilities, ct);

    public async Task<PagedResult<CountryListItemDto>> QueryAsync(TableRequest request, CancellationToken ct = default)
    {
        var page = await _api.PostJsonAsync("/api/Countries/query", request, ClientJsonContext.Default.TableRequest,
            ClientJsonContext.Default.CountryPage, ct).ConfigureAwait(false);
        if (page.Items is null || page.Page < 1 || page.PageSize != request.PageSize || page.TotalCount < 0 ||
            page.Items.Count > page.PageSize || page.Items.Any(x => x is null || x.Id == Guid.Empty))
            throw new InvalidDataException("The server returned an invalid country page.");
        return page;
    }
    public Task<TableFindResult> FindAsync(TableFindRequest request, CancellationToken ct = default) =>
        _api.PostJsonAsync("/api/Countries/find", request, ClientJsonContext.Default.TableFindRequest,
            ClientJsonContext.Default.TableFindResult, ct);
    public Task<string[]> FilterValuesAsync(TableValuesRequest request, CancellationToken ct = default) =>
        _api.PostJsonAsync("/api/Countries/filter-values", request, ClientJsonContext.Default.TableValuesRequest,
            ClientJsonContext.Default.StringArray, ct);

    private static void ValidateDetails(CountryDetailsDto country, Guid expectedId)
    {
        if (country.Id != expectedId || string.IsNullOrWhiteSpace(country.Code) ||
            string.IsNullOrWhiteSpace(country.Name))
            throw new InvalidDataException("The API returned invalid country details.");
    }

    public async Task<IReadOnlyList<CountryListItemDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var countries = await _api.GetJsonAsync(
            "/api/Countries", ClientJsonContext.Default.Countries, cancellationToken)
            .ConfigureAwait(false);

        foreach (var country in countries)
        {
            if (country is null || country.Id == Guid.Empty ||
                country.Code is null || country.Name is null)
            {
                throw new InvalidDataException("The API returned an invalid country item.");
            }
        }

        return countries;
    }
}
