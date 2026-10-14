using CorporateStarter.Application.Common.Interfaces.Security;
using CorporateStarter.Application.Common.Interfaces.Repositories.Security;
using CorporateStarter.Core.Entities.Security;
using CorporateStarter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace CorporateStarter.Tests.Integration.Auth;

public sealed class ProfilePersistenceTests(CorporateStarterApiFactory factory) : IClassFixture<CorporateStarterApiFactory>
{
    [Fact]
    public async Task Password_validation_is_atomic_and_success_revokes_sessions_and_preserves_other_users()
    {
        using var scope = factory.Services.CreateScope(); var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>(); var hasher = sp.GetRequiredService<IPasswordHasher>(); var service = sp.GetRequiredService<IMyProfileService>();
        const string old = "Initial123!Strong", next = "Changed456!Strong";
        var id = Guid.NewGuid(); var otherId = Guid.NewGuid();
        db.Users.Add(new() { Id = id, Login = "profile-" + id.ToString("N"), Email = id + "@example.test", PasswordHash = hasher.Hash(old), IsActive = true });
        db.Users.Add(new() { Id = otherId, Login = "other-" + otherId.ToString("N"), Email = otherId + "@example.test", PasswordHash = hasher.Hash(old), IsActive = true });
        var now = DateTime.UtcNow;
        var session = new AuthSession { Id = Guid.NewGuid(), UserId = id, CreatedAtUtc = now, LastSeenAtUtc = now, LastUserActivityAtUtc = now };
        var otherSession = new AuthSession { Id = Guid.NewGuid(), UserId = otherId, CreatedAtUtc = now, LastSeenAtUtc = now, LastUserActivityAtUtc = now };
        var family = new RefreshTokenFamily { Id = Guid.NewGuid(), UserId = id, AuthSessionId = session.Id, CreatedAtUtc = now };
        var token = new RefreshToken { Id = Guid.NewGuid(), UserId = id, AuthSessionId = session.Id, RefreshTokenFamilyId = family.Id, TokenHash = Guid.NewGuid().ToString("N"), CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(1) };
        db.AuthSessions.AddRange(session, otherSession); db.RefreshTokenFamilies.Add(family); db.RefreshTokens.Add(token); await db.SaveChangesAsync();
        Assert.True(await service.UpdateAppearanceAsync(id, new() { Theme = "Dark", PageSize = 50, TimeZone = "System" }, default));
        Assert.True(await service.UpdateAccountAsync(id, new() { DisplayName = " Alice " }, default));
        db.ChangeTracker.Clear();
        var profile = await service.GetAsync(id, default); Assert.Equal("Alice", profile!.Account.DisplayName); Assert.Equal(50, profile.Appearance.PageSize);
        Assert.Equal("profile.current_password_invalid", await service.ChangePasswordAsync(id, new() { CurrentPassword = "wrong", NewPassword = next }, default));
        Assert.Equal("profile.password_policy", await service.ChangePasswordAsync(id, new() { CurrentPassword = old, NewPassword = "weak" }, default));
        Assert.Equal("profile.password_reused", await service.ChangePasswordAsync(id, new() { CurrentPassword = old, NewPassword = old }, default));
        Assert.False(await db.AuthSessions.Where(x => x.Id == session.Id).Select(x => x.RevokedAtUtc != null).SingleAsync());
        Assert.Null(await service.ChangePasswordAsync(id, new() { CurrentPassword = old, NewPassword = next }, default));
        db.ChangeTracker.Clear();
        Assert.True(hasher.Verify(next, (await db.Users.SingleAsync(x => x.Id == id)).PasswordHash));
        Assert.NotNull((await db.AuthSessions.SingleAsync(x => x.Id == session.Id)).RevokedAtUtc);
        Assert.NotNull((await db.RefreshTokenFamilies.SingleAsync(x => x.Id == family.Id)).RevokedAtUtc);
        Assert.NotNull((await db.RefreshTokens.SingleAsync(x => x.Id == token.Id)).RevokedAtUtc);
        Assert.Null((await db.AuthSessions.SingleAsync(x => x.Id == otherSession.Id)).RevokedAtUtc);
        Assert.Equal(2, await db.PasswordHistories.CountAsync(x => x.UserId == id));
        Assert.Equal("profile.password_reused", await service.ChangePasswordAsync(id, new() { CurrentPassword = next, NewPassword = old }, default));
        Assert.True(await db.SecurityEvents.AnyAsync(x => x.SubjectUserId == id && x.EventType == "PasswordChanged"));
    }
}
