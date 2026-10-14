using System.Data;
using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Application.Common.Security;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Shared.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
namespace CorporateStarter.Infrastructure.Persistence.Repositories.Security;

public sealed class MyProfileService(AppDbContext db, IAuthService auth, IPasswordHasher hasher, IPasswordPolicyValidator policy,
    PasswordHistoryOptions history, PasswordPolicyOptions options) : IMyProfileService
{
    public async Task<MyProfileDto?> GetAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(x => x.Person).ThenInclude(x => x!.Company)
            .Include(x => x.Person).ThenInclude(x => x!.Position).SingleOrDefaultAsync(x => x.Id == userId && x.IsActive, ct);
        if (user is null) return null;
        var account = await auth.GetCurrentUserAsync(userId, ct);
        if (account is null) return null;
        var requirements = new List<string> { $"{options.MinimumLength}-{options.MaximumLength} characters" };
        if (options.RequireUppercase) requirements.Add("uppercase letter");
        if (options.RequireLowercase) requirements.Add("lowercase letter");
        if (options.RequireDigit) requirements.Add("digit");
        if (options.RequireNonAlphanumeric) requirements.Add("special character");
        return new()
        {
            Account = account,
            PersonId = user.PersonId,
            PersonName = user.Person is null ? null : string.Join(" ", new[] { user.Person.FirstName, user.Person.MiddleName, user.Person.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
            CompanyName = user.Person?.Company?.Name,
            PositionName = user.Person?.Position?.Name,
            Appearance = new() { Theme = user.PreferredTheme, PageSize = user.PreferredPageSize, TimeZone = user.PreferredTimeZone },
            PasswordRules = string.Join(", ", requirements) + ". Avoid your login, email and recently used passwords."
        };
    }
    public async Task<bool> UpdateAccountAsync(Guid id, UpdateMyAccountRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct); if (user is null) return false;
        user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return true;
    }
    public async Task<bool> UpdateAppearanceAsync(Guid id, ProfileSettingsDto request, CancellationToken ct)
    {
        if (!request.IsValid()) throw new ArgumentException("Invalid appearance settings.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct); if (user is null) return false;
        user.PreferredTheme = request.Theme; user.PreferredPageSize = request.PageSize; user.PreferredTimeZone = request.TimeZone;
        user.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return true;
    }
    // A serializable transaction makes hash/history/session changes atomic. Conflicts are returned for explicit retry.
    public async Task<string?> ChangePasswordAsync(Guid id, ChangeMyPasswordRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (user is null) return "profile.unavailable";
        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash)) return "profile.current_password_invalid";
        if (!policy.Validate(request.NewPassword, user.Login, user.Email).IsValid) return "profile.password_policy";
        var hashes = await db.PasswordHistories.Where(x => x.UserId == id).OrderByDescending(x => x.CreatedAtUtc)
            .Take(history.HistoryCount).Select(x => x.PasswordHash).ToListAsync(ct);
        if (hasher.Verify(request.NewPassword, user.PasswordHash) || hashes.Any(x => hasher.Verify(request.NewPassword, x))) return "profile.password_reused";
        var now = DateTime.UtcNow;
        // Older accounts (including the seed administrator) may not yet have a history entry.
        if (history.HistoryCount > 0 && !hashes.Contains(user.PasswordHash))
            db.PasswordHistories.Add(new() { Id = Guid.NewGuid(), UserId = id, PasswordHash = user.PasswordHash, CreatedAtUtc = now.AddMilliseconds(-1) });
        user.PasswordHash = hasher.Hash(request.NewPassword); user.UpdatedAtUtc = now;
        db.PasswordHistories.Add(new() { Id = Guid.NewGuid(), UserId = id, PasswordHash = user.PasswordHash, CreatedAtUtc = now });
        foreach (var session in await db.AuthSessions.Where(x => x.UserId == id && x.RevokedAtUtc == null).ToListAsync(ct)) session.RevokedAtUtc = now;
        foreach (var family in await db.RefreshTokenFamilies.Where(x => x.UserId == id && x.RevokedAtUtc == null).ToListAsync(ct)) family.RevokedAtUtc = now;
        foreach (var token in await db.RefreshTokens.Where(x => x.UserId == id && x.RevokedAtUtc == null).ToListAsync(ct)) token.RevokedAtUtc = now;
        db.SecurityEvents.Add(new()
        {
            EventType = "PasswordChanged",
            Severity = "Medium",
            Outcome = "Success",
            SubjectUserId = id,
            SubjectUserEmail = user.Email,
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return null;
    }
}
