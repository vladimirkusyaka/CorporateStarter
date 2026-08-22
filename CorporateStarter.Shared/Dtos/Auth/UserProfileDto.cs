namespace CorporateStarter.Shared.Dtos.Auth
{
    public sealed class UserProfileDto
    {
        public Guid Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public IReadOnlyList<UserRoleProfileDto> Roles { get; set; } = [];
    }
}
