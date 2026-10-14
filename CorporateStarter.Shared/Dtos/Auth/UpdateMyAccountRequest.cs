using System.ComponentModel.DataAnnotations;
namespace CorporateStarter.Shared.Dtos.Auth;

public sealed class UpdateMyAccountRequest
{
    [StringLength(256)] public string? DisplayName { get; set; }
}
