namespace CorporateStarter.Shared.Dtos.Security.Users
{
    public sealed class UserListItemDto
    {
        public Guid Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public bool IsActive { get; set; }

        public Guid? PersonId { get; set; }

        public string? PersonName { get; set; }

        public IReadOnlyList<UserRoleDto> Roles { get; set; } = [];
    }
}
