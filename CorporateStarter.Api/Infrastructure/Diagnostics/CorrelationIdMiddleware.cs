namespace CorporateStarter.Api.Infrastructure.Diagnostics
{
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        public const string ItemKey = "CorrelationId";

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context);

            context.Items[ItemKey] = correlationId;
            context.Response.Headers[HeaderName] = correlationId;

            await _next(context);
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var value))
            {
                var candidate = value.ToString();

                if (IsValidCorrelationId(candidate))
                    return candidate;
            }

            return Guid.NewGuid().ToString("N");
        }

        private static bool IsValidCorrelationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value.Length > 100)
                return false;

            return value.All(x =>
                char.IsLetterOrDigit(x) ||
                x is '-' or '_' or '.');
        }
    }
}
