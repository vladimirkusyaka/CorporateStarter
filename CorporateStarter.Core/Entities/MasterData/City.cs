using CorporateStarter.Core.Entities.Common;
using CorporateStarter.Core.Entities.Directory;

namespace CorporateStarter.Core.Entities.MasterData;

public class City : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Region { get; set; }

    public Guid CountryId { get; set; }
    public Country Country { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public ICollection<Company> Companies { get; set; } = new List<Company>();
}