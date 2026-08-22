using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public static class RateLimitPartitionKeyHelper
    {
        public static string GetClientIp(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        public static string HashPartitionValue(string value)
        {
            var normalized = value.Trim().ToUpperInvariant();
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

            return Convert.ToHexString(bytes);
        }
    }
}
