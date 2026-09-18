using CorporateStarter.Api.Infrastructure.Auth;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CorporateStarter.Api.Controllers;

[ApiController]
[Route("api/Auth/session")]
[AllowAnonymous]
[RequireCsrf]
[EnableRateLimiting("SessionActivity")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SessionActivityController(
    AppDbContext db, IRefreshTokenService tokens,
    RefreshTokenCookieHelper cookies, SessionIdlePolicy idlePolicy) : ControllerBase
{
    [HttpPost("status")]
    public Task<IActionResult> Status(CancellationToken token) => ReadAsync(false, token);

    [HttpPost("activity")]
    public Task<IActionResult> Activity(CancellationToken token) => ReadAsync(true, token);

    private async Task<IActionResult> ReadAsync(bool activity, CancellationToken token)
    {
        // Cookie credentials remain in the browser. The access JWT may have expired.
        var raw = cookies.Read(Request);
        if (string.IsNullOrWhiteSpace(raw) ||
            !Guid.TryParse(Request.Headers["X-Session-User"], out var expectedUser))
            return Unauthorized();

        var hash = tokens.HashToken(raw);
        var now = DateTime.UtcNow;
        var cutoff = now - idlePolicy.Timeout;
        var sessions = db.AuthSessions.Where(s =>
            s.UserId == expectedUser && s.User.IsActive &&
            s.RevokedAtUtc == null && s.LastUserActivityAtUtc > cutoff &&
            s.RefreshTokens.Any(t => t.TokenHash == hash &&
                t.RevokedAtUtc == null && t.ExpiresAtUtc > now &&
                t.RefreshTokenFamily.RevokedAtUtc == null));

        if (activity)
        {
            // Conditional UPDATE: an expired session cannot be resurrected.
            // Never move time backwards if requests arrive out of order.
            await sessions.Where(s => s.LastUserActivityAtUtc < now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.LastUserActivityAtUtc, now), token);
        }

        var state = await sessions.AsNoTracking()
            .Select(s => new { s.UserId, s.LastUserActivityAtUtc })
            .SingleOrDefaultAsync(token);
        if (state is null) return Unauthorized();

        var remaining = (state.LastUserActivityAtUtc + idlePolicy.Timeout -
            DateTime.UtcNow).TotalMilliseconds;
        if (remaining <= 0) return Unauthorized();
        return Ok(new { state.UserId, remainingMilliseconds = remaining });
    }
}
