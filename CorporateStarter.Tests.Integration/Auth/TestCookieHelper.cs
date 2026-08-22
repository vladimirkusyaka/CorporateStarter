using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Tests.Integration.Auth
{
    internal static class TestCookieHelper
    {
        public static string GetCookieValue(
            HttpResponseMessage response,
            string cookieName)
        {
            var setCookieHeaders = response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values
                : [];

            foreach (var header in setCookieHeaders)
            {
                var parts = header.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var nameValue = parts.FirstOrDefault(x => x.StartsWith(cookieName + "=", StringComparison.Ordinal));

                if (nameValue is null)
                {
                    continue;
                }

                return nameValue[(cookieName.Length + 1)..];
            }

            throw new InvalidOperationException($"Cookie '{cookieName}' was not found.");
        }

        public static void AddCookie(
            HttpRequestMessage request,
            string cookieName,
            string cookieValue)
        {
            request.Headers.Add("Cookie", $"{cookieName}={cookieValue}");
        }

        public static void AddCookies(
            HttpRequestMessage request,
            params (string Name, string Value)[] cookies)
        {
            request.Headers.Add(
                "Cookie",
                string.Join("; ", cookies.Select(x => $"{x.Name}={x.Value}")));
        }
    }
}
