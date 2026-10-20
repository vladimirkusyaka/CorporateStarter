namespace CorporateStarter.Shared.Dtos.Auth;

public sealed class PasswordPolicyInfo
{
    public int MinimumLength { get; set; }
    public int MaximumLength { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireDigit { get; set; }
    public bool RequireNonAlphanumeric { get; set; }
}
