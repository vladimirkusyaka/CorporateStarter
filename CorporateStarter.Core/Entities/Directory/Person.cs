using CorporateStarter.Core.Entities.Common;
using CorporateStarter.Core.Entities.MasterData;
using CorporateStarter.Core.Entities.Security;

namespace CorporateStarter.Core.Entities.Directory;

public class Person : BaseEntity
{
    public string? Code { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }

    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
}