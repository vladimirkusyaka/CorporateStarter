using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using CorporateStarter.Application.Common.Interfaces.Security;

namespace CorporateStarter.Infrastructure.Security
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private const int TokenSizeInBytes = 64;

        public string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);

            return Base64UrlEncode(bytes);
        }

        public string HashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
