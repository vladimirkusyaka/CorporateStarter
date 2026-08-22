namespace CorporateStarter.Application.Common.Security
{
    public sealed class JwtTokenResult
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public string JwtId { get; set; } = string.Empty;

        public Guid? AuthSessionId { get; set; }
    }
}
