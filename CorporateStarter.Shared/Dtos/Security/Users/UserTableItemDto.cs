namespace CorporateStarter.Shared.Dtos.Security.Users;

// Flat projection for the shared SQL table; details keep the structured role list.
public sealed class UserTableItemDto
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
}
