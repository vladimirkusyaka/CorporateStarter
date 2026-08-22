namespace CorporateStarter.Shared.Dtos.Security.Roles
{
    public class RoleListItemDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsSystemRole { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
