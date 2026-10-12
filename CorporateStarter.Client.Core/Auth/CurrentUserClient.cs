using CorporateStarter.Client.Core.Api;
using CorporateStarter.Client.Core.Serialization;
using CorporateStarter.Shared.Dtos.Auth;
using System.Globalization;
namespace CorporateStarter.Client.Core.Auth;

public sealed class CurrentUserClient(ClientApiClient api)
{
    public async Task<UserProfileDto> GetAsync(Guid expectedUserId, CancellationToken ct = default)
    {
        var profile = await api.GetJsonAsync("/api/Auth/me", ClientJsonContext.Default.UserProfileDto, ct);
        if (profile.Id != expectedUserId || profile.Id == Guid.Empty) throw new InvalidDataException("Unexpected user profile.");
        return profile;
    }
    public static string Name(UserProfileDto profile) => !string.IsNullOrWhiteSpace(profile.DisplayName)
        ? profile.DisplayName.Trim() : profile.Login.Trim();
    public static string Initial(string name) => string.IsNullOrWhiteSpace(name) ? "?"
        : StringInfo.GetNextTextElement(name.Trim()).ToUpperInvariant();
}
