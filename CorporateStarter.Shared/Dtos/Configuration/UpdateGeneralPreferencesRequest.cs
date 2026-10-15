namespace CorporateStarter.Shared.Dtos.Configuration;

public sealed class UpdateGeneralPreferencesRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string Name { get; set; } = "";
    public Guid Revision { get; set; }
}
