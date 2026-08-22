using System.Security.Cryptography;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class CsrfTokenService
    {
        private const int TokenSizeInBytes = 32;

        public string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public bool FixedTimeEquals(string left, string right)
        {
            var leftBytes = System.Text.Encoding.UTF8.GetBytes(left);
            var rightBytes = System.Text.Encoding.UTF8.GetBytes(right);

            return leftBytes.Length == rightBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }
    }
}
