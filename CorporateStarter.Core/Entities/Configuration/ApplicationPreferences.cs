namespace CorporateStarter.Core.Entities.Configuration;

public sealed class ApplicationPreferences
{
    public int Id { get; set; } = 1;
    public string Name { get; set; } = "CorporateStarter";
    public Guid Revision { get; set; }
}
