using System.ComponentModel.DataAnnotations;
namespace CorporateStarter.Shared.Dtos.Auth;

public sealed class ProfileSettingsDto
{
    [Required, RegularExpression("^(System|Light|Dark)$")] public string Theme { get; set; } = "System";
    [Range(10, 50)] public int PageSize { get; set; } = 20;
    [Required, RegularExpression("^(UTC|System)$")] public string TimeZone { get; set; } = "UTC";
    public bool IsValid() => Theme is "System" or "Light" or "Dark" && PageSize is 10 or 20 or 50 && TimeZone is "UTC" or "System";
}
