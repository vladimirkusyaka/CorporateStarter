using CorporateStarter.Client.Abstractions.Auth;
using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Auth;
using CorporateStarter.Client.Core.MasterData.Countries;
using CorporateStarter.Client.Core.MasterData.Positions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CorporateStarter.Client.Core;

public static class ClientCoreServiceCollectionExtensions
{
    // Each host supplies IClientApiTransport and the login/session/logout transports.
    // Neither business clients nor table state depend on the browser implementation.
    public static IServiceCollection AddCorporateStarterClientCore(this IServiceCollection services)
    {
        services.TryAddScoped<ClientAuthStateStore>();
        services.TryAddScoped<IClientAuthState>(sp => sp.GetRequiredService<ClientAuthStateStore>());
        services.TryAddScoped<ClientSessionCoordinator>();
        services.TryAddScoped<ClientApiClient>();
        services.TryAddScoped<CountriesClient>();
        services.TryAddScoped<PositionsClient>();
        services.TryAddScoped<CorporateStarter.Client.Core.Security.Permissions.PermissionsClient>();
        services.TryAddScoped<CorporateStarter.Client.Core.MasterData.Cities.CitiesClient>();
        return services;
    }
}
