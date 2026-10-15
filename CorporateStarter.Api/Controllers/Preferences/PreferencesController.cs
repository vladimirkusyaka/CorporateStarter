using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CorporateStarter.Application.Common.Interfaces.Repositories.Preferences;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Shared.Common;
using CorporateStarter.Shared.Dtos.Configuration;

namespace CorporateStarter.Api.Controllers;

[ApiController, Authorize, Route("api/preferences")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PreferencesController(IPreferencesRepository store, IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<PreferencesDto> Get(CancellationToken ct)
    {
        var general = await store.GetAsync(ct);
        var assembly = typeof(PreferencesController).Assembly;
        return new()
        {
            Name = general.Name,
            Revision = general.Revision,
            Environment = environment.EnvironmentName,
            ApiVersion = BuildInformation.Version(assembly),
            ApiBuildUtc = BuildInformation.BuiltAtUtc(assembly),
            CanUpdate = User.HasClaim("permission", AppPermissions.PreferencesUpdate),
            CanViewConnections = User.HasClaim("permission", AppPermissions.PreferencesConnectionsRead)
        };
    }

    [HttpPut("general"), Authorize(Policy = AppPermissions.PreferencesUpdate)]
    public async Task<ActionResult<GeneralPreferencesDto>> Update(UpdateGeneralPreferencesRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Any(char.IsControl) || request.Revision == Guid.Empty)
            return BadRequest(new { errorCode = "preferences.invalid_name" });
        var result = await store.UpdateAsync(request, ct);
        return result is null ? Conflict(new { errorCode = "preferences.conflict" }) : Ok(result);
    }

    [HttpGet("connections"), Authorize(Policy = AppPermissions.PreferencesConnectionsRead)]
    public ConnectionInfoDto Connections() => store.GetConnections();
}
