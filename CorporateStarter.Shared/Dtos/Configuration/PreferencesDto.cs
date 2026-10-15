namespace CorporateStarter.Shared.Dtos.Configuration;

public sealed class PreferencesDto
{
    public string Name { get; set; } = "";
    public Guid Revision { get; set; }
    public string Environment { get; set; } = "";
    public string ApiVersion { get; set; } = "";
    public DateTimeOffset? ApiBuildUtc { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanViewConnections { get; set; }
}
