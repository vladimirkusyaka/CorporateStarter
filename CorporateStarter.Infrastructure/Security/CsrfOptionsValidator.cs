using CorporateStarter.Application.Common.Security;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Infrastructure.Security
{
    public sealed class CsrfOptionsValidator : IValidateOptions<CsrfOptions>
    {
        public ValidateOptionsResult Validate(
            string? name,
            CsrfOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.CookieName))
            {
                failures.Add("CSRF cookie name is required.");
            }

            if (string.IsNullOrWhiteSpace(options.HeaderName))
            {
                failures.Add("CSRF header name is required.");
            }

            if (string.Equals(
                    options.CookieName,
                    options.HeaderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("CSRF cookie name and header name must be different.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}

