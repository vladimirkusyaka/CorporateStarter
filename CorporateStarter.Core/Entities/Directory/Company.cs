using CorporateStarter.Core.Entities.Common;
using CorporateStarter.Core.Entities.MasterData;

namespace CorporateStarter.Core.Entities.Directory;

public class Company : BaseEntity
{
    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? LegalName { get; set; }

    public string? TaxNumber { get; set; }

    public string? VatId { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Website { get; set; }

    public string? Street { get; set; }

    public string? HouseNumber { get; set; }

    public string? PostalCode { get; set; }

    public Guid? CityId { get; set; }
    public City? City { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Person> People { get; set; } = [];
}