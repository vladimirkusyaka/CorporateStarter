namespace CorporateStarter.Shared.Dtos.MasterData.Cities;

public class CityListItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Region { get; set; }

    public Guid CountryId { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}