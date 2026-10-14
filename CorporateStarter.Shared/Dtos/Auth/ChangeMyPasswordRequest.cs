using System.ComponentModel.DataAnnotations;
namespace CorporateStarter.Shared.Dtos.Auth;

public sealed class ChangeMyPasswordRequest
{
    [Required, StringLength(128)] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128)] public string NewPassword { get; set; } = "";
}
