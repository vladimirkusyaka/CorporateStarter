using System.ComponentModel.DataAnnotations;
namespace CorporateStarter.Shared.Dtos.Auth;

public sealed class MyProfileDto
{
    public UserProfileDto Account { get; set; } = new();
    public Guid? PersonId { get; set; }
    public string? PersonName { get; set; }
    public string? CompanyName { get; set; }
    public string? PositionName { get; set; }
    public ProfileSettingsDto Appearance { get; set; } = new();
    public string PasswordRules { get; set; } = "";
}
