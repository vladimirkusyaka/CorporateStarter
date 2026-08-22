using CorporateStarter.Core.Entities.Common;
using CorporateStarter.Core.Entities.Directory;

namespace CorporateStarter.Core.Entities.Security;

public class User : BaseEntity
{
    public string Login { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? PersonId { get; set; }

    public Person? Person { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public ICollection<AuthSession> AuthSessions { get; set; } = [];

    public ICollection<RefreshTokenFamily> RefreshTokenFamilies { get; set; } = [];
}