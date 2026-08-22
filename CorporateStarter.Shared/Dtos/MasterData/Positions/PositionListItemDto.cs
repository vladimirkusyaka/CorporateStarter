namespace CorporateStarter.Shared.Dtos.MasterData.Positions;

public class PositionListItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
