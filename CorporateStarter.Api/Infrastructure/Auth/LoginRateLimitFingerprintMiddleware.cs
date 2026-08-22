using System.Text.Json;

namespace CorporateStarter.Api.Infrastructure.Auth
{
    public sealed class LoginRateLimitFingerprintMiddleware
    {
        public const string ItemKey = "LoginRateLimitFingerprint";

        private readonly RequestDelegate _next;

        public LoginRateLimitFingerprintMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsLoginRequest(context))
            {
                await _next(context);
                return;
            }

            context.Request.EnableBuffering();

            try
            {
                using var document = await JsonDocument.ParseAsync(
                    context.Request.Body,
                    cancellationToken: context.RequestAborted);

                if (document.RootElement.TryGetProperty("login", out var loginElement))
                {
                    var login = loginElement.GetString();

                    if (!string.IsNullOrWhiteSpace(login))
                    {
                        context.Items[ItemKey] =
                            RateLimitPartitionKeyHelper.HashPartitionValue(login);
                    }
                }
            }
            catch (JsonException)
            {
                context.Items[ItemKey] = "invalid-json";
            }
            finally
            {
                context.Request.Body.Position = 0;
            }

            await _next(context);
        }

        private static bool IsLoginRequest(HttpContext context)
        {
            return HttpMethods.IsPost(context.Request.Method) &&
                   context.Request.Path.Equals(
                       "/api/Auth/login",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
