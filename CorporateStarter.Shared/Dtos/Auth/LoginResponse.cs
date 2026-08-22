namespace CorporateStarter.Shared.Dtos.Auth
{
    public sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public UserProfileDto User { get; set; } = new();
    }
}
