using System.Security.Claims;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;
namespace CorporateStarter.Api.Controllers;

[ApiController, Authorize, Route("api/profile")]
public sealed class MyProfileController(IMyProfileService service) : ControllerBase
{
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    [HttpGet]
    public async Task<ActionResult<MyProfileDto>> Get(CancellationToken ct)
    {
        if (UserId is not { } id) return Unauthorized();
        var profile = await service.GetAsync(id, ct); return profile is null ? Unauthorized() : Ok(profile);
    }
    [HttpPut("account")]
    public async Task<ActionResult<ProfileCommandReply>> Account(UpdateMyAccountRequest request, CancellationToken ct)
    {
        if (UserId is not { } id) return Unauthorized();
        return await service.UpdateAccountAsync(id, request, ct) ? Ok(new ProfileCommandReply(true)) : Unauthorized();
    }
    [HttpPut("appearance")]
    public async Task<ActionResult<ProfileCommandReply>> Appearance(ProfileSettingsDto request, CancellationToken ct)
    {
        if (UserId is not { } id) return Unauthorized();
        if (!request.IsValid()) return BadRequest(new { errorCode = "profile.invalid_settings" });
        return await service.UpdateAppearanceAsync(id, request, ct) ? Ok(new ProfileCommandReply(true)) : Unauthorized();
    }
    [HttpPost("password"), EnableRateLimiting("AuthIp")]
    public async Task<ActionResult<ProfileCommandReply>> Password(ChangeMyPasswordRequest request, CancellationToken ct)
    {
        if (UserId is not { } id) return Unauthorized();
        try
        {
            var error = await service.ChangePasswordAsync(id, request, ct);
            return error is null ? Ok(new ProfileCommandReply(true)) : BadRequest(new { errorCode = error });
        }
        catch (Exception ex) when (ex is PostgresException { SqlState: "40001" or "40P01" } || ex.InnerException is PostgresException { SqlState: "40001" or "40P01" })
        { return Conflict(new { errorCode = "profile.concurrent_change" }); }
    }
}
