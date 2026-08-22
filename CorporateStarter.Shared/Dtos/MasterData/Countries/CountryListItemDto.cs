namespace CorporateStarter.Shared.Dtos.MasterData.Countries;

public class CountryListItemDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NativeName { get; set; }

    public string? PhoneCode { get; set; }

    public bool IsActive { get; set; }
}
