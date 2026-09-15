using CorporateStarter.Api.Infrastructure.Auth;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Infrastructure.Security;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CorporateStarter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ServiceFilter(typeof(AuthOperationContentionFilter))]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly RefreshTokenCookieHelper _refreshTokenCookieHelper;
    private readonly CsrfCookieHelper _csrfCookieHelper;
    private readonly CsrfTokenService _csrfTokenService;
    private readonly AuthOperationExecutor _authOperationExecutor;

    public AuthController(IAuthService authService, RefreshTokenCookieHelper refreshTokenCookieHelper,
                    CsrfCookieHelper csrfCookieHelper,
                    CsrfTokenService csrfTokenService,
                    AuthOperationExecutor authOperationExecutor)
    {
        _authService = authService;
        _refreshTokenCookieHelper = refreshTokenCookieHelper;
        _csrfCookieHelper = csrfCookieHelper;
        _csrfTokenService = csrfTokenService;
        _authOperationExecutor = authOperationExecutor;

    }

    [AllowAnonymous]
    [EnableRateLimiting("AuthLogin")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MfaChallengeResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
    [FromBody] LoginRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (result is null)
            return Unauthorized();

        if (result.RequiresMfa)
            return Accepted(result.MfaChallenge);

        var session = result.Session;

        if (session is null)
            return Unauthorized();

        _refreshTokenCookieHelper.Append(
            Response,
            session.RefreshToken,
            session.RefreshTokenExpiresAtUtc);

        var csrfToken = _csrfTokenService.GenerateToken();
        _csrfCookieHelper.Append(
            Response,
            csrfToken,
            session.RefreshTokenExpiresAtUtc);

        return Ok(session.Response);
    }

    [RequireCsrf]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRefresh")]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Refresh(
        CancellationToken cancellationToken)
    {
        var refreshToken = _refreshTokenCookieHelper.Read(Request);

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        var result = await _authOperationExecutor.ExecuteAsync(
            (service, token) => service.RefreshAsync(
                refreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                token),
            cancellationToken);

        if (result is null)
        {
            _refreshTokenCookieHelper.Delete(Response);
            return Unauthorized();
        }

        _refreshTokenCookieHelper.Append(
            Response,
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc);

        var csrfToken = _csrfTokenService.GenerateToken();
        _csrfCookieHelper.Append(
            Response,
            csrfToken,
            result.RefreshTokenExpiresAtUtc);

        return Ok(result.Response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> Me(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);

        if (user is null)
            return Unauthorized();

        return Ok(user);
    }

    [EnableRateLimiting("AuthIp")]
    [RequireCsrf]
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = _refreshTokenCookieHelper.Read(Request);

        await _authOperationExecutor.ExecuteAsync(
            (service, token) => service.LogoutAsync(
                refreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                token),
            cancellationToken);

        _refreshTokenCookieHelper.Delete(Response);
        _csrfCookieHelper.Delete(Response);

        return NoContent();
    }

    [EnableRateLimiting("AuthIp")]
    [RequireCsrf]
    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        await _authOperationExecutor.ExecuteAsync(
            (service, token) => service.LogoutAllAsync(
                userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                token),
            cancellationToken);

        _refreshTokenCookieHelper.Delete(Response);
        _csrfCookieHelper.Delete(Response);

        return NoContent();
    }

    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<AuthSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var sessions = await _authService.GetActiveSessionsAsync(
            userId,
            GetCurrentAuthSessionId(),
            cancellationToken);

        return Ok(sessions);
    }

    [Authorize]
    [HttpDelete("sessions/{authSessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RevokeSession(
        Guid authSessionId,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var revoked = await _authOperationExecutor.ExecuteAsync(
            (service, token) => service.RevokeSessionAsync(
                userId,
                authSessionId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                token),
            cancellationToken);

        return revoked ? NoContent() : NotFound();
    }




    private Guid? GetCurrentAuthSessionId()
    {
        var value = User.FindFirst(JwtTokenService.AuthSessionIdClaimType)?.Value;

        return Guid.TryParse(value, out var authSessionId)
            ? authSessionId
            : null;
    }
}
