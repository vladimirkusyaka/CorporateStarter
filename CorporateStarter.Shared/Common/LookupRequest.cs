using System.ComponentModel.DataAnnotations;
namespace CorporateStarter.Shared.Common;

public sealed class LookupRequest
{
    [StringLength(200)] public string? Text { get; set; }
    [Range(1, 50)] public int Limit { get; set; } = 20;
}
