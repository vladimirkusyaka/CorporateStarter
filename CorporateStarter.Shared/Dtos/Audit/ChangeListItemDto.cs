namespace CorporateStarter.Shared.Dtos.Audit;

// The table does not download potentially large before/after JSON snapshots.
public sealed class ChangeListItemDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string? UserEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
