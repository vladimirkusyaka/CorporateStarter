using CorporateStarter.Application.Common.Security;
using Microsoft.Extensions.Options;

namespace CorporateStarter.Infrastructure.Security
{
    public sealed class RefreshTokenOptionsValidator : IValidateOptions<RefreshTokenOptions>
    {
        public ValidateOptionsResult Validate(
            string? name,
            RefreshTokenOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.CookieName))
            {
                failures.Add("Refresh token cookie name is required.");
            }

            if (options.LifetimeDays <= 0)
            {
                failures.Add("Refresh token expiration days must be greater than zero.");
            }

            if (options.LifetimeDays > 90)
            {
                failures.Add("Refresh token expiration days must not exceed 90 days.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}
