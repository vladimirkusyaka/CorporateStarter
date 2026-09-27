using System.ComponentModel.DataAnnotations;
namespace CorporateStarter.Shared.Dtos.MasterData.Positions;

public sealed class UpdatePositionRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
