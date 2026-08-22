using CorporateStarter.Core.Entities.Common;
using CorporateStarter.Core.Entities.Directory;

namespace CorporateStarter.Core.Entities.MasterData;

public class Position : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Person> People { get; set; } = new List<Person>();
}