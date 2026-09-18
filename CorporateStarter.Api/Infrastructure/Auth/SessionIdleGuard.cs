using CorporateStarter.Application.Common.Security;
using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CorporateStarter.Api.Infrastructure.Auth;

public sealed class SessionIdleGuard(AppDbContext db, SessionIdlePolicy idlePolicy)
{
    public Task<bool> IsActiveAsync(Guid sessionId, Guid userId, CancellationToken token)
    {
        var cutoff = DateTime.UtcNow - idlePolicy.Timeout;
        return db.AuthSessions.AsNoTracking().AnyAsync(s =>
            s.Id == sessionId && s.UserId == userId && s.RevokedAtUtc == null &&
            s.User.IsActive && s.LastUserActivityAtUtc > cutoff, token);
    }
}
