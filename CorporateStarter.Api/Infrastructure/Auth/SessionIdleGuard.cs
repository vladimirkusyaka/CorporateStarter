using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CorporateStarter.Api.Infrastructure.Auth;

public sealed class SessionIdleGuard(AppDbContext db, SessionIdlePolicy idlePolicy)
{
    public Task<bool> IsActiveAsync(
    Guid sessionId,
    Guid userId,
    CancellationToken token)
    {
        var sessions = db.AuthSessions
            .AsNoTracking()
            .Where(s =>
                s.Id == sessionId &&
                s.UserId == userId &&
                s.RevokedAtUtc == null &&
                s.User.IsActive);

        if (idlePolicy.IsEnabled)
        {
            var cutoff = DateTime.UtcNow - idlePolicy.Timeout;

            sessions = sessions.Where(s =>
                s.LastUserActivityAtUtc > cutoff);
        }

        return sessions.AnyAsync(token);
    }
}
