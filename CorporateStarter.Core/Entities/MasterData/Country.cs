using CorporateStarter.Core.Entities.Common;

namespace CorporateStarter.Core.Entities.MasterData;

public class Country : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? NativeName { get; set; }

    public string? PhoneCode { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<City> Cities { get; set; } = [];
}