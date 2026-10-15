using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Shared.Dtos.Configuration;
namespace CorporateStarter.Client.Core.Configuration;

public sealed class PreferencesClient(ClientApiClient api, IClientAuthState auth, ApplicationBranding branding)
{
    public async Task<PreferencesDto> GetAsync(CancellationToken ct = default)
    {
        var start = auth.Current;
        var result = await api.GetJsonAsync("/api/preferences", ClientJsonContext.Default.PreferencesDto, ct);
        CheckSession(start, ct);
        branding.Apply(result.Name);
        return result;
    }
    public async Task<GeneralPreferencesDto> UpdateAsync(UpdateGeneralPreferencesRequest request, CancellationToken ct = default)
    {
        var start = auth.Current;
        var result = await api.PutJsonAsync("/api/preferences/general", request,
            ClientJsonContext.Default.UpdateGeneralPreferencesRequest, ClientJsonContext.Default.GeneralPreferencesDto, ct);
        CheckSession(start, ct);
        branding.Apply(result.Name);
        return result;
    }
    public Task<ConnectionInfoDto> GetConnectionsAsync(CancellationToken ct = default) =>
        api.GetJsonAsync("/api/preferences/connections", ClientJsonContext.Default.ConnectionInfoDto, ct);

    private void CheckSession(ClientAuthSnapshot start, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var now = auth.Current;
        if (now.UserId is null || now.UserId != start.UserId || now.SessionGeneration != start.SessionGeneration)
            throw new OperationCanceledException("The preferences session changed.");
    }
}
