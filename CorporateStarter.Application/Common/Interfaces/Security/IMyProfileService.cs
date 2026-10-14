using CorporateStarter.Shared.Dtos.Auth;
namespace CorporateStarter.Application.Common.Interfaces.Security;

public interface IMyProfileService
{
    Task<MyProfileDto?> GetAsync(Guid userId, CancellationToken ct);
    Task<bool> UpdateAccountAsync(Guid userId, UpdateMyAccountRequest request, CancellationToken ct);
    Task<bool> UpdateAppearanceAsync(Guid userId, ProfileSettingsDto request, CancellationToken ct);
    Task<string?> ChangePasswordAsync(Guid userId, ChangeMyPasswordRequest request, CancellationToken ct);
}
