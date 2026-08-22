using CorporateStarter.Shared.Dtos.Auth;

namespace CorporateStarter.Application.Common.Security
{
    public sealed class UserAuthInfo
    {
        public Guid Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public IReadOnlyList<UserRoleProfileDto> Roles { get; set; } = [];
    }
}
